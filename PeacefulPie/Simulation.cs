using System.Collections.Generic;
using UnityEngine;

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

	[Header("Size of each simulation step, in seconds of game time.")]
	public float SimulationStepSize = 0.02f;
	[Header("Size of each simulation step, in real time. Make this smaller than Simulation Step Size to accelerate the game. Must be more than 0.")]
	public float RealStepSize = 0.005f;
	[Tooltip(
		"Automatically run a simulation step each FixedStep? " +
		"When under Python control, we usually want the Python will control when to run a simulation step, so this should be off.")]
	public bool AutoRunSimulations = false;

	List<INeedFixedUpdate> registeredNeedFixedUpdates = new List<INeedFixedUpdate>();
	List<INeedUpdate> registeredNeedUpdates = new List<INeedUpdate>();

	private void OnValidate() {
		if(RealStepSize < 0.001f) {
			RealStepSize = 0.005f;
		}
		Time.fixedDeltaTime = RealStepSize;
	}

	private void Awake() {
		Debug.Log("Turning off physics autosimulation");
		Physics.autoSimulation = false;
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
