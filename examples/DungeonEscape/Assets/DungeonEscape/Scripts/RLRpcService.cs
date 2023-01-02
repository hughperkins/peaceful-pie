using System.Collections.Generic;
using AustinHarris.JsonRpc;
using UnityEngine;

public class RLResult {
	// holds
	// the observation from the environmet, and any reward
	public RLResult() {

	}
	public RLResult(
		float reward,
		bool episodeFinished,
		List<PlayerObservation> playerObservations
	) {
		this.reward = reward;
		this.episodeFinished = episodeFinished;
		this.playerObservations = playerObservations;
	}
	public float reward;
	public bool episodeFinished;
	public List<PlayerObservation> playerObservations;
}

public class RLRpcService : MonoBehaviour {
	class RpcService : JsonRpcService {
        private DungeonEscapeEnvController gameManager;
		private Simulation simulation;
		private RLRpcService rlRpcService;

		public int frameSkip = 4;

        public RpcService(
				RLRpcService rlRpcService,
				DungeonEscapeEnvController gameManager,
				Simulation simulation) {
			this.gameManager = gameManager;
			this.rlRpcService = rlRpcService;
			this.simulation = simulation;
		}

		[JsonRpcMethod]
		RLResult rlStep(List<PlayerAction> actions) {
			// TODO, use value from Simulation. Maybe control simulation from here, so we can do accelreate etc?
			float rlDtTime = 0.02f;

			RLResult res = new RLResult();
			for(int i = 0; i < frameSkip + 1; i++) {
				gameManager.Step(actions, rlDtTime);
				simulation.Simulate();
			}
			res = gameManager.GetRLResult();
			return res;
		}
		[JsonRpcMethod]
		RLResult reset() {
			return gameManager.Reset();
		}
	}

	DungeonEscapeEnvController gameManager;
	Simulation simulation;
	RpcService rpcService;

	void Start()
	{
		gameManager = GetComponent<DungeonEscapeEnvController>();
		simulation = GetComponent<Simulation>();
		rpcService = new RpcService(this, gameManager, simulation);
	}
}
