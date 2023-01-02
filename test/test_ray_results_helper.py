import numpy as np
import pytest
from numpy.typing import NDArray

from peaceful_pie import ray_results_helper


@pytest.mark.parametrize(
    "distances,object_types,num_object_types,expected_results",
    [
        (
            [0.1, 0.25, 0.4, -1],
            [0, 2, 1, -1],
            3,
            np.array(
                [[10, 0, 0, 0], [0, 0, 1 / 0.4, 0], [0, 1 / 0.25, 0, 0]],
                dtype=np.float32,
            ),
        ),
        (
            [[0.1, 0.25, 0.5], [0.4, -1, 0.2]],
            [[0, 2, -1], [1, -1, 1]],
            3,
            np.array(
                [
                    [[10, 0, 0], [0, 0, 0]],
                    [[0, 0, 0], [1 / 0.4, 0, 5]],
                    [[0, 1 / 0.25, 0], [0, 0, 0]],
                ],
                dtype=np.float32,
            ),
        ),
    ],
)
def test_ray_results_to_feature_np(
    distances: list[list[float]],
    object_types: list[list[int]],
    num_object_types: int,
    expected_results: NDArray[np.float32],
) -> None:
    """
    _ray_results_to_vec(distances: list[list[float]], object_types: list[list[int]]) -> NDArray[np.float32]
    """
    actual = ray_results_helper.ray_results_to_feature_np(
        ray_results_helper.RayResults(
            NumObjectTypes=num_object_types,
            rayDistances=distances,
            rayHitObjectTypes=object_types,
        )
    )
    print("num_object_types", num_object_types)
    print("input shape", np.array(distances).shape)
    print("expected", expected_results, expected_results.shape)
    print("actual", actual, actual.shape)
    assert np.all(actual == expected_results)
