import argparse
from functools import partial
from os import path
from typing import Optional

import mlflow_logging
import torch
from sbs3_checkpoint_callback import SBS3CheckpointCallback
from stable_baselines3 import PPO
from stable_baselines3.common.policies import ActorCriticPolicy
from stable_baselines3.common.vec_env import SubprocVecEnv, VecMonitor, VecEnv
from stable_baselines3.common.monitor import Monitor

from peaceful_pie.unity_comms import UnityComms

import models
from my_unity_env import MyUnityEnv


def dump_params_counts(net: torch.nn.Module) -> int:
    total_params = 0
    for key, param in net.named_parameters():
        print(key, param.numel())
        total_params += param.numel()
    print("total_params", total_params)
    return total_params


def run(args: argparse.Namespace) -> None:
    class EnvFactory:
        def __init__(self, port: int, server_executable_path: Optional[str]):
            self.port = port
            self.server_executable_path = server_executable_path

        def __call__(self) -> MyUnityEnv:
            unity_comms = UnityComms(
                port=self.port,
                server_executable_path=self.server_executable_path)
            my_unity_env = MyUnityEnv(comms=unity_comms)
            return my_unity_env

    env_factories = [
        EnvFactory(port=port, server_executable_path=args.server_executable_path)
        for port in args.ports
    ]
    my_env: VecEnv
    if len(args.ports) > 1:
        my_env = SubprocVecEnv(env_fns=env_factories)  # type: ignore
        my_env = VecMonitor(my_env)
    else:
        my_env = env_factories[0]()
        my_env = Monitor(my_env)

    # default policy network is:
    # policy ActorCriticPolicy(
    #   (features_extractor): FlattenExtractor(
    #     (flatten): Flatten(start_dim=1, end_dim=-1)
    #   )
    #   (mlp_extractor): MlpExtractor(
    #     (shared_net): Sequential()
    #     (policy_net): Sequential(
    #       (0): Linear(in_features=276, out_features=64, bias=True)
    #       (1): Tanh()
    #       (2): Linear(in_features=64, out_features=64, bias=True)
    #       (3): Tanh()
    #     )
    #     (value_net): Sequential(
    #       (0): Linear(in_features=276, out_features=64, bias=True)
    #       (1): Tanh()
    #       (2): Linear(in_features=64, out_features=64, bias=True)
    #       (3): Tanh()
    #     )
    #   )
    #   (action_net): Linear(in_features=64, out_features=12, bias=True)
    #   (value_net): Linear(in_features=64, out_features=1, bias=True)
    # )
    policy_kwargs = {
        "default": None,
        "3x128:relu": dict(
            activation_fn=torch.nn.ReLU,
            net_arch=[dict(pi=[128, 128, 128], vf=[128, 128, 128])],
        ),
        "3x256:relu": dict(
            activation_fn=torch.nn.ReLU,
            net_arch=[dict(pi=[256, 256, 256], vf=[256, 256, 256])],
        ),
        "3x512:relu": dict(
            activation_fn=torch.nn.ReLU,
            net_arch=[dict(pi=[512, 512, 512], vf=[512, 512, 512])],
        ),
        "sharedpolicy": None,
        "sharedboth": None
    }[args.policy_network]
    policy: str | ActorCriticPolicy | partial[ActorCriticPolicy]
    match args.policy_network:
        case "sharedpolicy":
            policy = partial(models.MyPolicy, PolicyNetwork=models.MySharedNetworkPolicy)
        case "sharedboth":
            policy = partial(models.MyPolicy, PolicyNetwork=models.MySharedNetworkBoth)
        case _:
            policy = "MlpPolicy"
    ppo = PPO(
        env=my_env,
        policy=policy,  # type: ignore
        learning_rate=0.0002,
        ent_coef=args.ent_reg,
        verbose=1,
        policy_kwargs=policy_kwargs,
    )
    print("policy", ppo.policy)
    assert ppo.policy is not None
    total_params = dump_params_counts(ppo.policy)
    mlflow_logger = None
    if not args.no_mlflow:
        params_to_log = {"num_params": total_params}
        mlflow_logger = mlflow_logging.MLFlowLogger(
            experiment_name="unityml",
            run_name=args.ref,
            params_to_log=params_to_log,
        )
        mlflow_logger.log_args(args)
        loggers = mlflow_logging.create_loggers(mlflow_logger=mlflow_logger)
        ppo.set_logger(loggers)
    callback = SBS3CheckpointCallback(
        mlflow_logger=mlflow_logger,
        n_steps=ppo.n_steps,
        iterations_multiplier=args.checkpoint_epochs_multiplier,
        save_path=path.join("models", args.ref),
    )
    ppo.learn(total_timesteps=10000000, callback=callback)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--ports", type=int, nargs='+', default=[9000],
        help='Provide more than one to run against multiple unity processes.')
    parser.add_argument('--server-executable-path', type=str, help='optional path to dedicated server executable')
    parser.add_argument("--ref", type=str, required=True)
    parser.add_argument("--no-mlflow", action="store_true")
    parser.add_argument(
        "--policy-network",
        type=str,
        default="sharedboth",
        choices=["default", "3x128:relu", "3x256:relu", "3x512:relu", "sharedpolicy", "sharedboth"],
    )
    parser.add_argument(
        "--checkpoint-epochs-multiplier",
        type=float,
        default=2,
        help="checkpoint epochs is power of this multiplier and checkpoint index",
    )
    parser.add_argument('--ent-reg', default=0.001, type=float)
    args = parser.parse_args()
    run(args)
