using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AustinHarris.JsonRpc;
using UnityEngine;


class NetworkEvent {
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
	[Tooltip("Network port to listen on.")]
	public int ListenPort = 9000;
	[Tooltip("Network address to listen on. If not sure, put 'localhost'")]
	public string ListenAddress = "localhost";
	[Tooltip("Optional filepath to write logs to.")]
	public string? LogFilepath;
	[Tooltip("milliseconds to sleep after writing to logfile. Helps prevent runaway logs")]
	public int SleepAfterLogLineMilliseconds = 100;
	[Tooltip("Ensure the application runs in background. If you set this to false, you might wonder why your python scripts appears to hang")]
	public bool AutoEnableRunInBackground = true;
	[Tooltip("If set to true, than we listen for network requested with a blocking listen. " +
		"This will run *much* faster than non-blocking, " +
		"but be aware if the python stops sending, then the editor will freeze, and the only " +
		"solution (other than restarting a python sender), is to Force Quit the Unity " +
		"Editor. (Best to set this programatically, on command from the python client, and have the client set back " +
		"to non-blocking, when it exits")]
	public bool blockingListen = false;

	HttpListener? listener;
	BlockingCollection<NetworkEvent> networkEvents = new BlockingCollection<NetworkEvent>();

	void MyDebug(string msg) {
		if(LogFilepath != null && LogFilepath != "") {
			string DateTime = System.DateTime.Now.ToString("yyyyMMdd HH:mm:ss.fff");
			using(StreamWriter sw = File.AppendText(LogFilepath)) {
				sw.WriteLine($"{DateTime} {msg}");
			}
			Thread.Sleep(SleepAfterLogLineMilliseconds);
		}
	}

	bool isDedicated;
	volatile bool isEnabled = true;

	class RpcService : JsonRpcService {
		// this is only for turning blocking on/off
		NetManager netManager;
		public RpcService(NetManager netManager) {
			this.netManager = netManager;
		}
		[JsonRpcMethod]
		private void setBlockingListen(bool blocking)
		{
			// if blocking is true, then we listen for requests from python
			// using a blocking listen. This will run faster than non-blocking,
			// but if the python stops sending, then the editor will freeze, and the only
			// solution (other than restarting a python sender), is to Force Quit the Unity
			// Editor.
			this.netManager.SetBlocking(blocking);
		}
	}

	RpcService? rpcService;

	private void Awake() {
		rpcService = new RpcService(this);
		if (AutoEnableRunInBackground) {
			Application.runInBackground = true;
			Debug.Log("Enabled Application.runInBackground");
		} else
		{
			Debug.Log("Warning: NOT enabling Application.runInBackground. This means you will need to switch Unity to foreground for things to run.");
		}
	}

	public void SetBlocking(bool blocking) {
		Debug.Log($"Setting blocking network listen to {blocking}");
		this.blockingListen = blocking;
	}

	public bool IsDedicated() {
		return Screen.currentResolution.refreshRate == 0;
	}

	public void OnEnable() {
		isEnabled = true;
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
		MyDebug($"Started listener, on address {ListenAddress} port {ListenPort}");
	}

	public void OnDisable() {
		isEnabled = false;
		Debug.Log("shutting down listener");
		if(listener is null) {
			return;
		}
		listener.Stop();
		listener.Abort();
		listener.Close();
	}

	void FixedUpdate() {
		int numIts = 0;
		// while (
		// 	(networkEvents.Count > 0 && numIts == 0)
		// 	|| (blockingListen && numIts < 10)
		// 	|| (isDedicated && numIts == 0)
		// ) {
		if(networkEvents.Count > 0 || blockingListen || isDedicated) {
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
		using(var reader = new StreamReader(req.InputStream, req.ContentEncoding)) {
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
		HttpListenerContext context = listener.GetContext();
		handleRequest(context);
	}

	async public void listenOnceAsync() {
		if(listener is null) {
			return;
		}
		HttpListenerContext context = await listener.GetContextAsync();
		handleRequest(context);
	}

	void listenLoop() {
		if (LogFilepath != null && LogFilepath != "")
		{
			File.OpenWrite(LogFilepath).Close();
		}
		while(true) {
			try {
				listenOnce();
			} catch(ObjectDisposedException) {
				MyDebug("objectdisposedexception");
				MyDebug($"isEnabled {isEnabled}");
				if(isEnabled) {
					MyDebug("Looks like we are supposed to be still active. Lets reopen...");
					try {
						if(listener != null) {
							listener.Abort();
						}
					} catch(Exception e) {
						MyDebug($"exception during attempted abort {e}");
						MyDebug($"(Ignoring exception))");
					}
					try {
						listener = new HttpListener();
						listener.Prefixes.Add($"http://{ListenAddress}:{ListenPort}/");

						listener.Start();
						MyDebug($"Restarted listener");
					} catch(Exception e) {
						MyDebug($"error when trying to restart listener {e}");
						throw e;
					}
				} else {
					// shutdown
					break;
				}
			} catch(System.Net.HttpListenerException e) {
				if(e.Message == "Listener closed") {
					MyDebug("listener closed");
					break;
				}
				MyDebug($"HttpListenerException caught in httplistener {e}");
			} catch(System.Threading.ThreadAbortException) {
				MyDebug("threadabortexception");
				break;
			} catch(Exception e) {
				MyDebug($"Exception caught in httplistener {e}");
			}
		}
		MyDebug("ListenLoop shut down.");
	}
}
