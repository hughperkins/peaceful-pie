import math
import os
from os import path
from typing import Optional

from stable_baselines3.common.callbacks import BaseCallback

from mlflow_logging import MLFlowLogger


class SBS3CheckpointCallback(BaseCallback):
    """
    This will save out checkpoints at exponentially increasing intervals, and upload them
    to mlflow

    the checkpoints will be created at iterations:

    e_0 = 1
    e_1 = 2
    ...
    e_i = math.ceil(math.pow(iterations_multiplier, i))
    """

    def __init__(
        self,
        mlflow_logger: Optional[MLFlowLogger],
        iterations_multiplier: float,
        n_steps: int,
        save_path: str,
        verbose: int = 1,
    ):
        super().__init__(verbose)
        self.iterations_multiplier = iterations_multiplier
        self.save_path = save_path
        self.mlflow_logger = mlflow_logger
        self.n_steps = n_steps

        # next save will be at epoch math.ceil(math.pow(self.next_checkpoint_idx, self.epochs_multiplier))
        self.next_checkpoint_idx = 0

    def _init_callback(self) -> None:
        if not path.isdir(self.save_path):
            os.makedirs(self.save_path)

    def _on_step(self) -> bool:
        iteration_idx = (
            self.n_calls - 1
        ) // self.n_steps  # try to get *after* the training :P
        if iteration_idx >= int(
            math.ceil(math.pow(self.iterations_multiplier, self.next_checkpoint_idx))
        ):
            model_path = path.join(self.save_path, f"ckp_{iteration_idx}.zip")
            assert self.model is not None
            self.model.save(model_path)
            if self.mlflow_logger is not None:
                self.mlflow_logger.upload_artifact(model_path)
            os.remove(model_path)
            print(
                f"uploaded checkpoint for iteration {iteration_idx} as {path.basename(model_path)}"
            )
            self.next_checkpoint_idx += 1
        return True
