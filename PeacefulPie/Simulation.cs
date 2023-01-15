using System.Collections.Generic;
using UnityEngine;
using AustinHarris.JsonRpc;

public interface INeedFixedUpdate {
	void MyFixedUpdate(float deltaTime);
	bool IsAlive();
}

public interface INeedUpdate {
	void MyUpdate(float deltaTime);
	bool IsAlive();
}

public class Simulation : MonoBehaviour {
	// handles running physics etc
	// will step automatically when no AI runnnig, otherwise on command of
	// rl rpc incoming step event

	[Tooltip("Size of each simulation step, in seconds of game time.")]
	public float SimulationStepSize = 0.02f;
	[Tooltip("tldr; Controls the minimum time between network requests. " +
	    "More detail: the PeacefulPie networking system hooks into Unity's event loop using FixedUpdate. " +
		"Thus Fixed Delta Time limits the number of network requests we can process a second. " +
		"Set this to smaller than Simulation Step Size to train faster than real time.")]
	public float FixedDeltaTime = 0.005f;
	[Tooltip("Click this to apply the Fixed Delta Time you set above, during runtime. " +
	    "(This is to avoid accidentally setting it to 0, which locks the editor)")]
	public bool ApplyFixedDeltaTime = false;
	[Tooltip("Size of Fixed Delta Time, in real time, on a dedicated server. Typically should be really small, e.g. 0.0001.")]
	public float DedicatedFixedDeltaTime = 0.0001f;
	[Tooltip("Application Target Frame Rate, on a dedicated server. Typically should be relatively small, such as 10")]
	public int DedicatedTargetFrameRate = 10;
	[Tooltip(
		"Automatically run a simulation step every FixedUpdate? " +
		"When under Python control, we usually want the Python client to control when to run a simulation step, so this should be off.")]
	public bool AutoRunSimulations = false;

	List<INeedFixedUpdate> registeredNeedFixedUpdates = new List<INeedFixedUpdate>();
	List<INeedUpdate> registeredNeedUpdates = new List<INeedUpdate>();

	Rpc? rpc;

	class Rpc : JsonRpcService {
		Simulation simulation;
		public Rpc(Simulation simulation)
		{
			this.simulation = simulation;
		}
		[JsonRpcMethod]
		void setAutosimulation(bool autosimulation) {
			simulation.AutoRunSimulations = autosimulation;
			Debug.Log($"Setting Simulation.AutoRunSimulations to {autosimulation}");
		}
		[JsonRpcMethod]
		bool getAutosimulation() {
			return simulation.AutoRunSimulations;
		}
	}

	public bool isDedicated() {
		return Screen.currentResolution.refreshRate == 0;
	}

	private void OnValidate() {
		if(ApplyFixedDeltaTime)
		{
			if(FixedDeltaTime == 0.0f) {
				Debug.LogError("FixedDeltaTime should not be 0");
			} else
			{
				Time.fixedDeltaTime = FixedDeltaTime;
				ApplyFixedDeltaTime = false;
				Debug.Log($"apply fixed delta time {Time.fixedDeltaTime}");
			}
		}
	}

	private void Awake() {
		Debug.Log("Turning off physics autosimulation");
		Physics.autoSimulation = false;

		rpc = new Rpc(this);

		Debug.Log("is dedicated " + isDedicated());
		if(isDedicated()) {
			Application.targetFrameRate = DedicatedTargetFrameRate;
			Debug.Log($"Set application target framerate to {Application.targetFrameRate}");
			FixedDeltaTime = DedicatedFixedDeltaTime;
		}
		Debug.Log($"Setting fixed delta time to {FixedDeltaTime}");
		Time.fixedDeltaTime = FixedDeltaTime;
	}

	public void RegisterNeedFixedUpdate(INeedFixedUpdate needFixedUpdate) {
		registeredNeedFixedUpdates.Add(needFixedUpdate);
	}

	public void RegisterNeedUpdate(INeedUpdate needUpdate) {
		registeredNeedUpdates.Add(needUpdate);
	}

	public void RunFixedUpdates(float deltaTime) {
		foreach(INeedFixedUpdate needsFixedUpdate in registeredNeedFixedUpdates) {
			if(needsFixedUpdate.IsAlive()) {
				needsFixedUpdate.MyFixedUpdate(deltaTime);
			}
		}
	}

	public void RunUpdates(float deltaTime) {
		foreach(INeedUpdate needsUpdate in registeredNeedUpdates) {
			if(needsUpdate.IsAlive()) {
				needsUpdate.MyUpdate(deltaTime);
			}
		}
	}

	void FixedUpdate() {
		if(AutoRunSimulations) {
			Simulate();
		}
	}

	public void Simulate() {
		float deltaTime = SimulationStepSize;
		RunFixedUpdates(deltaTime);
		RunUpdates(deltaTime);
		if(!Physics.autoSimulation) {
			Physics.Simulate(deltaTime);
		}
	}
}
