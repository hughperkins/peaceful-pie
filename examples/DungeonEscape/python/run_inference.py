import argparse
import time

import mlflow_utils
import my_unity_env
from stable_baselines3 import PPO

from peaceful_pie import unity_comms


def run(args: argparse.Namespace) -> None:
    mlflow_loader = mlflow_utils.MlflowLoader(
        experiment_name=args.experiment_name, mlflow_uri=args.mlflow_uri
    )

    checkpoint_path = mlflow_loader.download_checkpoint(args.run_name, args.iterations)

    comms = unity_comms.UnityComms(port=args.port)
    comms.rlInitAi(accel=args.accel, frame_skip=0)

    my_env = my_unity_env.MyUnityEnv(comms=comms)
    ppo = PPO.load(checkpoint_path, my_env)
    print(ppo.policy)

    obs = my_env.reset()
    step = 0
    while True:
        # print('stepping')
        actions, _states = ppo.predict(obs)
        for _ in range(args.frame_skip + 1):
            obs, rewards, dones, info = my_env.step(actions.tolist())
            if dones or step > 200:
                print("resetting")
                obs = my_env.reset()
                step = 0
                break
        step += 1
        print(f"\r{step}", end="", flush=True)
        time.sleep(1 / 50 / args.accel)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    # parser.add_argument("--checkpoint-path", type=str, required=True)
    parser.add_argument(
        "--run",
        type=str,
        required=True,
        help="format is: [run_name]:[iterations], eg foo:1024",
    )
    parser.add_argument("--experiment-name", type=str, default="unityml")
    parser.add_argument("--mlflow-uri", type=str, default="http://localhost:5000")
    parser.add_argument("--port", type=int, default=9000)
    parser.add_argument(
        "--frame-skip",
        type=int,
        default=4,
        help="should match what was used for training",
    )
    parser.add_argument("--accel", type=float, default=1.0)
    args = parser.parse_args()
    args.run_name, iterations = args.run.split(":")
    args.iterations = int(iterations)
    iterations = None
    run(args)
