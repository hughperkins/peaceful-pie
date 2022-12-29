using System.Collections.Generic;
using UnityEngine;

public interface INeedFixedUpdate{
    void MyFixedUpdate(float deltaTime);
    bool IsAlive();
}

public interface INeedUpdate{
    void MyUpdate(float deltaTime);
    bool IsAlive();
}

public class Simulation : MonoBehaviour {
    // handles running physics etc
    // will step automatically when no AI runnnig, otherwise on command of
    // rl rpc incoming step event

    [Header("Size of each simulation step, in seconds of game time.")]
    public float SimulationStepSize = 0.02f;

    List<INeedFixedUpdate> registeredNeedFixedUpdates = new List<INeedFixedUpdate>();
    List<INeedUpdate> registeredNeedUpdates = new List<INeedUpdate>();

    [HideInInspector]
    public bool AutoRunSimulations = true;

    void Awake() {
        Physics.autoSimulation = false;
    }

    public void RegisterNeedFixedUpdate(INeedFixedUpdate needFixedUpdate) {
        registeredNeedFixedUpdates.Add(needFixedUpdate);
    }

    public void RegisterNeedUpdate(INeedUpdate needUpdate) {
        registeredNeedUpdates.Add(needUpdate);
    }

    public void RunFixedUpdates(float deltaTime) {
        foreach(INeedFixedUpdate needsFixedUpdate in registeredNeedFixedUpdates) {
            if(needsFixedUpdate.IsAlive())  {
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
        // Debug.Log("simulation.simulate");
        float deltaTime = SimulationStepSize;
        RunFixedUpdates(deltaTime);
        RunUpdates(deltaTime);
        Physics.Simulate(deltaTime);
    }
}
