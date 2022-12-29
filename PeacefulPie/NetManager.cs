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
	public string serverReply;
	public AutoResetEvent serverReplied = new AutoResetEvent(false);
	public NetworkEvent(string clientRequest) {
		this.clientRequest = clientRequest;
	}
}



public class NetManager : MonoBehaviour {
	[Tooltip("Network port to listen on.")]
    public int ListenPort = 9000;
	[Tooltip("Network address to listen on. If not sure, put 'localhost'")]
    public string ListenAddress = "localhost";
	[Tooltip("Optional filepath to write logs to.")]
	public string? LogFilepath;
	[Tooltip("milliseconds to sleep after writing to logfile. Helps prevent runaway logs")]
	public int SleepAfterLogLineMilliseconds = 100;

	HttpListener listener;
	BlockingCollection<NetworkEvent> networkEvents = new BlockingCollection<NetworkEvent>();

	void MyDebug(string msg) {
		if(LogFilepath != null) {
			string DateTime = System.DateTime.Now.ToString("yyyyMMdd HH:mm:ss.fff"); 
			using (StreamWriter sw = File.AppendText(LogFilepath)) {
				sw.WriteLine($"{DateTime} {msg}");
			}
			Thread.Sleep(SleepAfterLogLineMilliseconds);
		}
	}

	bool isDedicated;

	public bool IsDedicated()
	{
		return Screen.currentResolution.refreshRate == 0;
	}

    public void OnEnable() {
		isDedicated = IsDedicated();

		string[] args = System.Environment.GetCommandLineArgs();
		for(int i = 0; i < args.Length; i++) {
			if(args[i] == "--port") {
				ListenPort = int.Parse(args[i + 1]);
				Debug.Log($"Using port {ListenPort}");
			} else if(args[i] == "--help") {
				Debug.Log("Specify port with '--port [port number]'");
				Application.Quit();
				return;
			}
		}

		listener = new HttpListener();
		listener.Prefixes.Add($"http://{ListenAddress}:{ListenPort}/");

		listener.Start();
		Task.Run(listenLoop);
    }

    public void OnDisable() {
		Debug.Log("shutting down listener");
		listener.Stop();
		listener.Abort();
		listener.Close();
	}

	void FixedUpdate() {
		int numIts = 0;
		if(networkEvents.Count > 0 || isDedicated) {
			NetworkEvent networkEvent = networkEvents.Take();
			networkEvent.serverReply = JsonRpcProcessor.ProcessSync(
				Handler.DefaultSessionId(), networkEvent.clientRequest, null);
			networkEvent.serverReplied.Set();
			numIts += 1;
		}
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
		string res = networkEvent.serverReply;

		using HttpListenerResponse resp = context.Response;
		resp.Headers.Set("Content-Type", "application/json");

		byte[] buffer = Encoding.UTF8.GetBytes(res);
		resp.ContentLength64 = buffer.Length;

		using Stream ros = resp.OutputStream;
		ros.Write(buffer, 0, buffer.Length);
	}

	public void listenOnce() {
		HttpListenerContext context;
		context = listener.GetContext();
		handleRequest(context);
	}

	async public void listenOnceAsync() {
		HttpListenerContext context;
		context = await listener.GetContextAsync();
		handleRequest(context);
	}

	void listenLoop()
	{
		File.OpenWrite("netmanager-log.txt").Close();
		while (true)
		{
			try {
				listenOnce();
			}
			catch(ObjectDisposedException) {
				MyDebug("objectdisposedexception");
				if(gameObject.activeSelf) {
					MyDebug("Looks like we are supposed to be still active. Lets reopen...");
					try {
						listener.Abort();
					}
					catch(Exception e) {
						MyDebug($"exception during attempted abort {e}");
						MyDebug($"(Ignoring exception))");
					}
					try {
						listener = new HttpListener();
						listener.Prefixes.Add($"http://{ListenAddress}:{ListenPort}/");

						listener.Start();
						MyDebug($"Restarted listener");
					}
					catch(Exception e) {
						MyDebug($"error when trying to restart listener {e}");
						throw e;
					}
				} else {
					// shutdown
					break;
				}
			}
			catch(System.Net.HttpListenerException e) {
				if(e.Message == "Listener closed") {
					MyDebug("listener closed");
					break;
				}
				MyDebug($"HttpListenerException caught in httplistener {e}");
			}
			catch(System.Threading.ThreadAbortException) {
				MyDebug("threadabortexception");
				break;
			}
			catch(Exception e) {
				MyDebug($"Exception caught in httplistener {e}");
			}
		}
	}
}
