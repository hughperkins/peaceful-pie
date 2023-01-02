using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAgent {
    PlayerObservation GetObservation();
    PlayerObservation GetDeadObservation();
	void Reset();
	void ApplyAction(PlayerAction action, float deltaTime);
}

public class DungeonEscapeEnvController : MonoBehaviour, INeedFixedUpdate
{
    [System.Serializable]
    public class PlayerInfo
    {
        public PushAgentEscape Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
        [HideInInspector]
        public Collider Col;
    }

    [System.Serializable]
    public class DragonInfo
    {
        public SimpleNPC Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
        [HideInInspector]
        public Collider Col;
        public Transform T;
        public bool IsDead;
    }

    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    /// <returns></returns>
    [Header("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;
    private int m_ResetTimer;

    /// <summary>
    /// The area bounds.
    /// </summary>
    [HideInInspector]
    public Bounds areaBounds;
    /// <summary>
    /// The ground. The bounds are used to spawn the elements.
    /// </summary>
    public GameObject ground;
    Simulation simulation;

    Material m_GroundMaterial; //cached on Awake()

    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>
    Renderer m_GroundRenderer;

    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();
    public List<DragonInfo> DragonsList = new List<DragonInfo>();
    private Dictionary<PushAgentEscape, PlayerInfo> m_PlayerDict = new Dictionary<PushAgentEscape, PlayerInfo>();
    public bool UseRandomAgentRotation = true;
    public bool UseRandomAgentPosition = true;
    PushBlockSettings m_PushBlockSettings;

    private int m_NumberOfRemainingPlayers;
    public GameObject Key;
    public GameObject Tombstone;

    [HideInInspector]
    public float Reward;
    public bool EpisodeFinished;
    public bool AIEngaged;
    void Start()
    {
		Application.runInBackground = true;
        simulation = GetComponent<Simulation>();
        simulation.RegisterNeedFixedUpdate(this);

        // Get the ground's bounds
        areaBounds = ground.GetComponent<Collider>().bounds;
        // Get the ground renderer so we can change the material when a goal is scored
        m_GroundRenderer = ground.GetComponent<Renderer>();
        // Starting material
        m_GroundMaterial = m_GroundRenderer.material;
        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();

        //Reset Players Remaining
        m_NumberOfRemainingPlayers = AgentsList.Count;

        //Hide The Key
        Key.SetActive(false);

        // Initialize TeamManager
        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            item.Col = item.Agent.GetComponent<Collider>();
        }
        foreach (var item in DragonsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.T = item.Agent.transform;
            item.Col = item.Agent.GetComponent<Collider>();
        }

        ResetScene();
    }

    void EndEpisode() {
        // Debug.Log("end episode");
        EpisodeFinished = true;
        if(!AIEngaged) {
            ResetScene();
        }
    }

    public bool IsAlive() {
        return gameObject.activeSelf;
    }

    public void MyFixedUpdate(float deltaTime)
    {
        m_ResetTimer += 1;
        // Reward -= 0.02f;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            EndEpisode();
            // ResetScene();
        }
    }

    public void TouchedHazard(PushAgentEscape agent)
    {
        m_NumberOfRemainingPlayers--;
        Debug.Log("vanished through portal 🌀");
        // Reward -= 0.2f;
        if (m_NumberOfRemainingPlayers == 0 || agent.IHaveAKey)
        {
            EndEpisode();
            // ResetScene();
        }
        else
        {
            agent.gameObject.SetActive(false);
        }
    }

    public void UnlockDoor()
    {
        StartCoroutine(GoalScoredSwapGroundMaterial(m_PushBlockSettings.goalScoredMaterial, 0.5f));

        print("Unlocked Door 🚪");
        Reward += 1;
        EndEpisode();

        // ResetScene();
    }

    public void KilledByBaddie(PushAgentEscape agent, Collision baddieCol)
    {
        baddieCol.gameObject.SetActive(false);
        m_NumberOfRemainingPlayers--;
        agent.gameObject.SetActive(false);
        print($"{baddieCol.gameObject.name} ate {agent.transform.name} 🍖");

        //Spawn Tombstone
        Tombstone.transform.SetPositionAndRotation(agent.transform.position, agent.transform.rotation);
        Tombstone.SetActive(true);

        //Spawn the Key Pickup
        Key.transform.SetPositionAndRotation(baddieCol.collider.transform.position, baddieCol.collider.transform.rotation);
        Key.SetActive(true);
    }

    /// <summary>
    /// Use the ground's bounds to pick a random spawn position.
    /// </summary>
    public Vector3 GetRandomSpawnPos()
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier);

            var randomPosZ = Random.Range(-areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier);
            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }

    /// <summary>
    /// Swap ground material, wait time seconds, then swap back to the regular material.
    /// </summary>
    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time); // Wait for 2 sec
        m_GroundRenderer.material = m_GroundMaterial;
    }

    public void BaddieTouchedBlock()
    {
        // Swap ground material for a bit to indicate we scored.
        StartCoroutine(GoalScoredSwapGroundMaterial(m_PushBlockSettings.failMaterial, 0.5f));
        Debug.Log("dragon won 🐲");
        // Reward -= 1;
        EndEpisode();
        // ResetScene();
    }

    Quaternion GetRandomRot()
    {
        return Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
    }

	// public void ApplyActions(List<PlayerAction> actions, float rlDeltaTime) {
	public void Step(List<PlayerAction> actions, float rlDeltaTime) {
        for(int i = 0; i < AgentsList.Count; i++) {
            PlayerInfo playerInfo = AgentsList[i];
		    playerInfo.Agent.ApplyAction(actions[i], rlDeltaTime);
        }
    }

    public RLResult GetRLResult() {
        // RunFixedUpdates(rlDeltaTime);
        List<PlayerObservation> observations = GetObservations();
        RLResult res = new RLResult(
            Reward,
            EpisodeFinished,
            observations
        );
        if(res.reward > 0) {
            Debug.Log($"reward {res.reward} 🏆");
        }
        if(res.reward < -0.1) {
            Debug.Log($"reward {res.reward}");
        }
        Reward = 0;
        return res;
	}

	List<PlayerObservation> GetObservations() {
        List<PlayerObservation> playerObservations = new List<PlayerObservation>();
        for(int i = 0; i < AgentsList.Count; i++) {
            PlayerInfo playerInfo = AgentsList[i];
            PlayerObservation obs;
            if(playerInfo.Agent.gameObject.activeSelf) {
    		    obs = playerInfo.Agent.GetObservation();
            } else {
                obs = playerInfo.Agent.GetDeadObservation();
            }
            playerObservations.Add(obs);
        }
		return playerObservations;
	}

    public RLResult Reset() {
        // Debug.Log("New game 🎮");
        Debug.Log("-------------- New game 🎮 ------------------");
        ResetScene();
        return new RLResult(
            0,
            false,
            GetObservations()
        );
    }

    void ResetScene()
    {
        //Reset counter
        m_ResetTimer = 0;
        Reward = 0;
        EpisodeFinished = false;

        //Reset Players Remaining
        m_NumberOfRemainingPlayers = AgentsList.Count;

        //Random platform rot
        var rotation = Random.Range(0, 4);
        var rotationAngle = rotation * 90f;
        transform.Rotate(new Vector3(0f, rotationAngle, 0f));

        //Reset Agents
        foreach (var item in AgentsList)
        {
            var pos = UseRandomAgentPosition ? GetRandomSpawnPos() : item.StartingPos;
            var rot = UseRandomAgentRotation ? GetRandomRot() : item.StartingRot;

            item.Agent.transform.SetPositionAndRotation(pos, rot);
            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
            item.Agent.MyKey.SetActive(false);
            item.Agent.IHaveAKey = false;
            item.Agent.gameObject.SetActive(true);
        }

        //Reset Key
        Key.SetActive(false);

        //Reset Tombstone
        Tombstone.SetActive(false);

        //End Episode
        foreach (var item in DragonsList)
        {
            if (!item.Agent)
            {
                return;
            }
            item.Agent.transform.SetPositionAndRotation(item.StartingPos, item.StartingRot);
            item.Agent.SetRandomWalkSpeed();
            item.Agent.gameObject.SetActive(true);
        }
    }
}
