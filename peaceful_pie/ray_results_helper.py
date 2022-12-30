from dataclasses import dataclass

import numpy as np
from numpy.typing import NDArray


@dataclass
class RayResults:
    rayDistances: list[list[float]]
    rayHitObjectTypes: list[list[int]]
    NumObjectTypes: int


def ray_results_to_feature_np(
    ray_results: RayResults,
) -> NDArray[np.float32]:
    """
    Takes in the ray results, i.e. hit distances and object types,
    and return a single numpy array, of same shape as each of distances and object types,
    but with an additional 0th dimension of length object_types
    the distances will be inverted, so that nearer gives a higher number, and further gives
    smaller. this number will be assigned to the output plane indexed by object type.
    If object type is -1, the number will be ignored, and output will be 0 for all output
    channels
    """
    distances_np = np.array(ray_results.rayDistances)
    distances_np = 1 / distances_np
    object_types_np = np.array(ray_results.rayHitObjectTypes)
    # add one feature plane for the object type of -1
    _obs = np.zeros(
        (ray_results.NumObjectTypes + 1, *distances_np.shape), dtype=np.float32
    )
    np.put_along_axis(
        _obs,
        np.expand_dims(object_types_np, axis=0),
        np.expand_dims(distances_np, axis=0),
        axis=0,
    )
    # truncate off the not found object feature plane
    _obs = _obs[:-1]
    return _obs
