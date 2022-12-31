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
	[Tooltip("Size of each simulation step, in real time. Make this smaller than Simulation Step Size to accelerate the game. Must be more than 0. " +
		"You need to click Apply Real Step Size below, to apply any changes at runtime.")]
	public float RealStepSize = 0.005f;
	[Tooltip("Click this to apply the Real Step Size you set above, during runtime. (This is to avoid accidentally setting it to 0)")]
	public bool ApplyRealStepSize = false;
	[Tooltip("Size of each simulation step, in real time, on a dedicated server. Typically should be really small, e.g. 0.0001.")]
	public float DedicatedRealStepSize = 0.0001f;
	[Tooltip("Number of Updates to run per second, on a dedicated server. Typically should be small, such as 10.")]
	public int DedicatedTargetFrameRate = 10;
	[Tooltip(
		"Automatically run a simulation step each FixedStep? " +
		"When under Python control, we usually want the Python will control when to run a simulation step, so this should be off.")]
	public bool AutoRunSimulations = false;

	List<INeedFixedUpdate> registeredNeedFixedUpdates = new List<INeedFixedUpdate>();
	List<INeedUpdate> registeredNeedUpdates = new List<INeedUpdate>();

	Rpc rpc;

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
	}

	public bool isDedicated() {
		return Screen.currentResolution.refreshRate == 0;
	}

	private void OnValidate() {
		if(ApplyRealStepSize)
		{
			if(RealStepSize == 0.0f) {
				Debug.LogError("RealStepSize should not be 0");
			} else
			{
				Time.fixedDeltaTime = RealStepSize;
				ApplyRealStepSize = false;
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
			RealStepSize = DedicatedRealStepSize;
			Debug.Log($"Set RealStepSize to {RealStepSize}");
		}
		Debug.Log($"Setting fixed delta time to {RealStepSize}");
		Time.fixedDeltaTime = RealStepSize;
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
