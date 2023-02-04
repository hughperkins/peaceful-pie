try:
    # Shut up protobuf deprecation warnings, "Call to deprecated create function" ...
    import google.protobuf.descriptor  # type: ignore

    google.protobuf.descriptor._Deprecated.count = 0  # type: ignore
except Exception:
    pass
from unittest import mock

import my_unity_env
import numpy as np
import pytest
from numpy.typing import NDArray

from peaceful_pie import ray_results_helper


@pytest.mark.parametrize(
    "player_obs,expected_vec",
    [
        (
            my_unity_env.PlayerObservation(
                True,
                False,
                ray_results_helper.RayResults(
                    rayDistances=[[1, 2]], rayHitObjectTypes=[[0, 1]], NumObjectTypes=2
                ),
            ),
            np.array([1, 0, 0, 0.5, 1, 0]),
        ),
        (
            my_unity_env.PlayerObservation(
                True,
                True,
                ray_results_helper.RayResults(
                    rayDistances=[[1, 2]], rayHitObjectTypes=[[0, 1]], NumObjectTypes=2
                ),
            ),
            np.array([1, 0, 0, 0.5, 1, 1]),
        ),
    ],
)
def test__player_observation_to_vec(
    player_obs: my_unity_env.PlayerObservation, expected_vec: NDArray[np.float32]
) -> None:
    unity_comms = mock.Mock()
    unity_comms.reset.return_value = my_unity_env.RLResult(0, False, [player_obs])
    env = my_unity_env.MyUnityEnv(unity_comms)
    actual = env._player_observation_to_vec(player_obs)
    print("expected", expected_vec, expected_vec.shape)
    print("actual", actual, actual.shape)
    assert np.all(actual == expected_vec)


@pytest.mark.parametrize(
    "rl_res,expected_vec",
    [
        (
            my_unity_env.RLResult(
                reward=0.782,
                episodeFinished=True,
                playerObservations=[
                    my_unity_env.PlayerObservation(
                        IAmAlive=True,
                        IHaveAKey=True,
                        rayResults=ray_results_helper.RayResults(
                            rayDistances=[[1, 2]],
                            rayHitObjectTypes=[[0, 1]],
                            NumObjectTypes=2,
                        ),
                    ),
                    my_unity_env.PlayerObservation(
                        IAmAlive=True,
                        IHaveAKey=False,
                        rayResults=ray_results_helper.RayResults(
                            rayDistances=[[1, 2]],
                            rayHitObjectTypes=[[0, 1]],
                            NumObjectTypes=2,
                        ),
                    ),
                ],
            ),
            np.array([1, 0, 0, 0.5, 1, 1, 1, 0, 0, 0.5, 1, 0]),
        )
    ],
)
def test_result_to_obs(
    rl_res: my_unity_env.RLResult, expected_vec: NDArray[np.float32]
) -> None:
    unity_comms = mock.Mock()
    unity_comms.reset.return_value = rl_res
    env = my_unity_env.MyUnityEnv(unity_comms)
    actual = env._result_to_obs(rl_res)
    print("expected", expected_vec, expected_vec.shape)
    print("actual", actual, actual.shape)
    assert np.all(actual == expected_vec)
