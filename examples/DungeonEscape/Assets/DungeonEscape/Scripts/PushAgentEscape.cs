using System.Collections.Generic;
using UnityEngine;

public class PlayerObservation {
    public PlayerObservation(
        bool IAmAlive,
        bool IHaveAKey,
        RayResults rayResults
    ) {
        this.IAmAlive = IAmAlive;
        this.IHaveAKey = IHaveAKey;
        this.rayResults = rayResults;
    }
    public bool IAmAlive;
    public bool IHaveAKey;
    public RayResults rayResults;
}

public enum PlayerAction {
    nop,
    forward,
    backward,
    rotateLeft,
    rotateRight,
    rotateUp,
    rotateDown,
    translateLeft,
    translateRight,
}

public class PushAgentEscape : MonoBehaviour, IAgent, INeedUpdate
{

    private Simulation simulation;
    public GameObject MyKey; //my key gameobject. will be enabled when key picked up.
    public bool IHaveAKey; //have i picked up a key
    private PushBlockSettings m_PushBlockSettings;
    private Rigidbody m_AgentRb;
    private DungeonEscapeEnvController m_GameController;

    RayCasts m_RayCasts;

    public void Awake()
    {
        m_GameController = GetComponentInParent<DungeonEscapeEnvController>();
        // m_GameController.RegisterNeedFixedUpdate(this);
        m_AgentRb = GetComponent<Rigidbody>();
        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();
        MyKey.SetActive(false);
        IHaveAKey = false;
        m_RayCasts = GetComponentInChildren<RayCasts>();
        simulation = GetComponentInParent<Simulation>();
        simulation.RegisterNeedUpdate(this);
    }

    public void MyUpdate(float deltaTime) {
        // Debug.Log("agent myupdate");
        // if(!m_GameController.AIEngaged) {
        //     PlayerAction action = PlayerAction.nop;
        //     if(Input.GetAxis("RotateX") > 0.1) {
        //         action = PlayerAction.rotateRight;
        //     } else if(Input.GetAxis("RotateX") < -0.1) {
        //         action = PlayerAction.rotateLeft;
        //     } else if(Input.GetAxis("Horizontal") > 0.1) {
        //         action = PlayerAction.translateRight;
        //     } else if(Input.GetAxis("Horizontal") < -0.1) {
        //         action = PlayerAction.translateLeft;
        //     } else if(Input.GetAxis("Vertical") < -0.1) {
        //         action = PlayerAction.backward;
        //     } else if(Input.GetAxis("Vertical") > 0.1) {
        //         action = PlayerAction.forward;
        //     }
        //     ApplyAction(action, deltaTime);
        // }
    }

    public PlayerObservation GetDeadObservation() {
        RayResults rayResults = m_RayCasts.GetZerodObservation();
        PlayerObservation obs = new PlayerObservation(
            false,
            false,
            rayResults
        );
        return obs;
    }

    public PlayerObservation GetObservation() {
        RayResults rayResults = m_RayCasts.GetObservation();
        PlayerObservation obs = new PlayerObservation(
            true,
            IHaveAKey,
            rayResults
        );
        return obs;
    }

    public bool IsAlive() {
        return gameObject.activeSelf;
    }

    public void Reset()
    {
        MyKey.SetActive(false);
        IHaveAKey = false;
    }

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
    // public void MoveAgent(PlayerAction action, float deltaTime)
    // {
    //     ApplyAction(action, Time.fixedDeltaTime);
    // }

    public void ApplyAction(PlayerAction action, float deltaTime) {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        switch (action)
        {
            case PlayerAction.nop:
                // do nothing
                break;
            case PlayerAction.forward:
                dirToGo = transform.forward * 1f;
                break;
            case PlayerAction.backward:
                dirToGo = transform.forward * -1f;
                break;
            case PlayerAction.rotateRight:
                rotateDir = transform.up * 1f;
                break;
            case PlayerAction.rotateLeft:
                rotateDir = transform.up * -1f;
                break;
            case PlayerAction.translateLeft:
                dirToGo = transform.right * -0.75f;
                break;
            case PlayerAction.translateRight:
                dirToGo = transform.right * 0.75f;
                break;
        }
        // if(rotateDir != Vector3.zero) {
        transform.Rotate(rotateDir, deltaTime * m_PushBlockSettings.agentRotationSpeed);
        // }
        // if(dirToGo != Vector3.zero) {
        m_AgentRb.AddForce(dirToGo * m_PushBlockSettings.agentRunSpeed,
            ForceMode.VelocityChange);
        // }
        // Debug.Log($"Time.fixedDeltaTime {Time.fixedDeltaTime}");
        // transform.Rotate(transform.up, deltaTime * 200f * m_PushBlockSettings.agentRotationSpeed);
        // transform.Rotate(transform.up, deltaTime * m_PushBlockSettings.agentRotationSpeed);
        // Vector3 appliedDirection = transform.forward;
        // Debug.Log($"agent forward {transform.forward} applieddirection {appliedDirection}");
        // m_AgentRb.AddForce(appliedDirection * m_PushBlockSettings.agentRunSpeed,
        //     ForceMode.VelocityChange);
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.transform.CompareTag("lock"))
        {
            if (IHaveAKey)
            {
                MyKey.SetActive(false);
                IHaveAKey = false;
                m_GameController.UnlockDoor();
            }
        }
        if (col.transform.CompareTag("dragon"))
        {
            m_GameController.KilledByBaddie(this, col);
            MyKey.SetActive(false);
            IHaveAKey = false;
        }
        if (col.transform.CompareTag("portal"))
        {
            m_GameController.TouchedHazard(this);
        }
    }

    void OnTriggerEnter(Collider col)
    {
        //if we find a key and it's parent is the main platform we can pick it up
        if (col.transform.CompareTag("key") && col.transform.parent == transform.parent && gameObject.activeInHierarchy)
        {
            print("Picked up key 🔑");
            MyKey.SetActive(true);
            IHaveAKey = true;
            col.gameObject.SetActive(false);
        }
    }
}
