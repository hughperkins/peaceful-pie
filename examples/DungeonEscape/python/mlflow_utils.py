import datetime
import os
import shutil
import time
from os.path import join
from typing import Optional

import mlflow
import mlflow.entities


class MlflowLoader:
    """
    Utilities for interacting with mlflow

    When a run_name is provided as input, if there are multiple runs with the same name,
    this class uses the run with the longest duration, or if there is a run currently running, with that
    name, then uses that run.

    You can either st MLFLOW_TRACKING_URI envornment variable with the url of your mlflow server, or
    pass the url in through the parameter mlflow_uri.
    """
    def __init__(self, experiment_name: str, mlflow_uri: Optional[str] = None):
        self.experiment_name = experiment_name
        self.cache_dir = ".mlflow_cache"
        if not os.path.exists(self.cache_dir):
            os.makedirs(self.cache_dir)

        if mlflow_uri is None:
            assert "MLFLOW_TRACKING_URI" in os.environ
            print("mlflow tracking uri", mlflow.get_tracking_uri())
            self.mlflow_uri = os.environ["MLFLOW_TRACKING_URI"]
        else:
            self.mlflow_uri = mlflow_uri

    def _get_checkpoint_cache_filename(self, run_name: str, run_iterations: int) -> str:
        return f"{run_name}:{run_iterations}.zip"

    def download_checkpoint(self, run_name: str, run_iterations: int) -> str:
        """
        Downloads a checkpoint from mlflow, given the run_name, and the number of
        iterations of training prior to the checkpoint. For example, to download
        ckp_1024.zip, then set run_iterations=1024.
        """
        cache_filename = self._get_checkpoint_cache_filename(
            run_name=run_name, run_iterations=run_iterations
        )
        cache_filepath = join(self.cache_dir, cache_filename)
        if os.path.exists(cache_filepath):
            print(f"returning checkpoint from cache for {run_name}:{run_iterations}")
            return cache_filepath
        print("downloading checkpoint...")
        run = self.get_run(run_name=run_name)
        self.download_artifact(
            run=run, artifact_path=f"ckp_{run_iterations}.zip", dest_path=cache_filepath
        )
        return cache_filepath

    def download_artifact(
        self, run: mlflow.entities.Run, artifact_path: str, dest_path: Optional[str]
    ) -> str:
        client = mlflow.MlflowClient(tracking_uri=self.mlflow_uri)
        run_id = run.info.run_id
        downloaded_path = None
        while downloaded_path is None:
            try:
                downloaded_path = client.download_artifacts(
                    run_id=run_id, path=artifact_path
                )
            except Exception as e:
                print("exception downloading from mlflow", e)
                print("trying again")
                time.sleep(1)
        if dest_path is not None:
            shutil.move(downloaded_path, dest_path)
            downloaded_path = dest_path
        return downloaded_path

    def get_run(self, run_name: str) -> mlflow.entities.Run:
        """
        Returns run with longest duration matching the experiment and run names
        """
        client = mlflow.MlflowClient(tracking_uri=self.mlflow_uri)
        experiment_id = None
        while experiment_id is None:
            try:
                experiment_id = client.get_experiment_by_name(
                    self.experiment_name
                ).experiment_id
            except Exception as e:
                print("exception", e, "whilst getting experiment id")
                print("retrying")
                time.sleep(1)
        print("experiment_id", experiment_id)
        runs = None
        while runs is None:
            try:
                runs = client.search_runs(
                    experiment_ids=[experiment_id],
                    filter_string=f"tags.mlflow.runName = '{run_name}'",
                )
            except Exception as e:
                print("exception", e, "whilst searching runs")
                print("retrying")
                time.sleep(1)
        longest_run = None
        longest_run_duration = None
        for run in runs:
            run_data: mlflow.entities.RunData = run.data
            if run.info.end_time is None:
                # assume that ongoing run is the one we want
                longest_run = run
                break
            duration = datetime.datetime.fromtimestamp(
                run.info.end_time / 1000
            ) - datetime.datetime.fromtimestamp(run.info.start_time / 1000)
            print(
                "duration",
                duration,
                "ep_len_mean",
                run_data.metrics.get("rollout/ep_len_mean", None),
            )
            if longest_run_duration is None or duration > longest_run_duration:
                longest_run_duration = duration
                longest_run = run
        return longest_run
