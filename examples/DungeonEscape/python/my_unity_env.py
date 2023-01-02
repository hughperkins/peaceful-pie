from dataclasses import dataclass
from typing import Any, Tuple

import gym
import gym.spaces
import numpy as np
from numpy.typing import NDArray

from peaceful_pie import ray_results_helper, unity_comms


@dataclass
class PlayerObservation:
    IAmAlive: bool
    IHaveAKey: bool
    rayResults: ray_results_helper.RayResults


@dataclass
class RLResult:
    reward: float
    episodeFinished: bool
    playerObservations: list[PlayerObservation]


class MyUnityEnv(gym.Env):
    def __init__(
        self,
        comms: unity_comms.UnityComms,
    ):
        self.comms = comms

        self.action_space = gym.spaces.MultiDiscrete(
            [4] * 3
        )  # turn left/right look up/down forward
        obs = self.reset()
        print("obs.shape", obs.shape)
        self.observation_space = gym.spaces.Box(
            low=0, high=1, shape=obs.shape, dtype=np.float32
        )

    def reset(self) -> NDArray[np.float32]:
        rl_result: RLResult = self.comms.reset(ResultClass=RLResult)
        obs = self._result_to_obs(rl_result)
        return obs

    def _player_observation_to_vec(
        self, player_obs: PlayerObservation
    ) -> NDArray[np.float32]:
        """
        Takes in a player observation, containing ray hits, and whether the player has a key,
        and returns a single vector, representing both of those
        """
        vec = ray_results_helper.ray_results_to_feature_np(player_obs.rayResults)
        res = np.concatenate(
            [vec.flatten(), [player_obs.IAmAlive, player_obs.IHaveAKey]]
        )
        return res

    def _result_to_obs(self, rl_result: RLResult) -> NDArray[np.float32]:
        return np.concatenate(
            [
                self._player_observation_to_vec(obs)
                for obs in rl_result.playerObservations
            ]
        )

    def step(
        self, actions: list[int]
    ) -> Tuple[NDArray[np.float32], float, bool, dict[str, Any]]:
        action_strs = [
            [
                "nop",
                "rotateLeft",
                "rotateRight",
                "forward",
            ][action]
            for action in actions
        ]
        rl_result: RLResult = self.comms.rlStep(actions=action_strs, ResultClass=RLResult)
        obs = self._result_to_obs(rl_result)
        info: dict[str, Any] = {"finished": rl_result.episodeFinished}
        return obs, rl_result.reward, rl_result.episodeFinished, info

    def close(self) -> None:
        ...
