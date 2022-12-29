using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AustinHarris.JsonRpc;
using UnityEngine;


class NetworkEvent{
	// contains request from client, space for reply from server,
	// and a thread signaling thing so client can wait for reply
	// we'll keep everything as json strings here, and do the conversion
	// in the main thread
	public string clientRequest;
	public string? serverReply;
	public AutoResetEvent serverReplied = new AutoResetEvent(false);
	public NetworkEvent(string clientRequest) {
		this.clientRequest = clientRequest;
	}
}



public class NetManager : MonoBehaviour {
    public int Port;

	HttpListener? listener;
	BlockingCollection<NetworkEvent> networkEvents = new BlockingCollection<NetworkEvent>();

	void MyDebug(string msg) {
		using (StreamWriter sw = File.AppendText("/tmp/log.txt")) {
			sw.WriteLine(msg);
		}
	}

	class RpcService : JsonRpcService {
		// this is only for turning sync on/off
		NetManager netManager;
		public RpcService(NetManager netManager) {
			this.netManager = netManager;
		}
		[JsonRpcMethod]
		private void stopUpdates()
		{
			// we stop running updates, just listen for network
			this.netManager.stopUpdates();
		}
		[JsonRpcMethod]
		private void startUpdates()
		{
			// restart running Update
			this.netManager.startUpdates();
		}
	}

	RpcService? rpcService;

	private bool updatesPaused;

    public void OnEnable() {
		rpcService = new RpcService(this);

		listener = new HttpListener();
		listener.Prefixes.Add($"http://localhost:{Port}/");

		listener.Start();
		Task.Run(listenLoop);
    }

    public void OnDisable() {
		Debug.Log("shutting down listener");
		if(listener is null) {
			return;
		}
		listener.Stop();
		listener.Abort();
		listener.Close();
	}

	void handleRequest(HttpListenerContext context) {
		HttpListenerRequest req = context.Request;

		string bodyText;
		using(var reader = new StreamReader(req.InputStream, req.ContentEncoding))
		{
			bodyText = reader.ReadToEnd();
		}

		NetworkEvent networkEvent = new NetworkEvent(bodyText);
		networkEvents.Add(networkEvent);
		networkEvent.serverReplied.WaitOne();
		
		string? res = networkEvent.serverReply;

		using HttpListenerResponse resp = context.Response;
		resp.Headers.Set("Content-Type", "application/json");

		byte[] buffer = Encoding.UTF8.GetBytes(res);
		resp.ContentLength64 = buffer.Length;

		using Stream ros = resp.OutputStream;
		ros.Write(buffer, 0, buffer.Length);
	}

	public void listenOnce() {
		if(listener is null) {
			return;
		}
		HttpListenerContext context;
		context = listener.GetContext();
		handleRequest(context);
	}

	async public void listenOnceAsync() {
		if(listener is null) {
			return;
		}
		HttpListenerContext context = await listener.GetContextAsync();
		handleRequest(context);
	}

	public void stopUpdates() {
		updatesPaused = true;
	}

	public void startUpdates() {
		updatesPaused = false;
	}

	void Update() {
		int numIts = 0;
		while(networkEvents.Count > 0 || (updatesPaused && numIts < 10)) {
			NetworkEvent networkEvent = networkEvents.Take();
			networkEvent.serverReply = JsonRpcProcessor.ProcessSync(
				Handler.DefaultSessionId(), networkEvent.clientRequest, null);
			networkEvent.serverReplied.Set();
			numIts += 1;
		}
	}

	void listenLoop()
	{
		while (true)
		{
			try {
				listenOnce();
			}
			catch(Exception) {
				using (StreamWriter sw = File.AppendText("/tmp/log.txt")) {
					sw.WriteLine("Exception caught in httplistener");
				}
			}
		}
	}
}
