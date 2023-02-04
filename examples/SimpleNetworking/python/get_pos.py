import argparse
from dataclasses import dataclass

from peaceful_pie.unity_comms import UnityComms


@dataclass
class MyVector3:
    x: float
    y: float
    z: float


def run(args: argparse.Namespace) -> None:
    unity_comms = UnityComms(port=args.port)
    res: MyVector3 = unity_comms.getPosition(ResultClass=MyVector3)
    print("res", res, "res.x", res.x)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, default=9000)
    args = parser.parse_args()
    run(args)
