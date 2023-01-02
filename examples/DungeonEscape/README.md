# Dungeon Escape

Tested using peaceful-pie 1.4.0

This runs WITHOUT mlagents. It does not use POCA, but my own experients showed that this configuration learns faster and better than the POCA configuration provided by mlagents in https://github.com/Unity-Technologies/ml-agents/blob/5b6cb9878c611d5c50585f6400ebb8fe16ee8f1f/config/poca/DungeonEscape.yaml

We train using stable baselines3 PPO.

The network architecture is in [python/models.py](python/models.py). It is the class `MySharedNetworkBoth`.
- we share the same policy network across all three agents
- For the value network, we run the observation from each agent through the same value network, then we concatenate these results, and pass through one final layer.
