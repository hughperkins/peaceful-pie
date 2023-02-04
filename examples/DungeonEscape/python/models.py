from typing import Any, Callable, Dict, List, Optional, Tuple, Type, Union

import gym
import gym.spaces
import torch
from stable_baselines3.common.policies import ActorCriticPolicy
from stable_baselines3.common.type_aliases import Schedule
from torch import nn


class MySharedNetworkPolicy(nn.Module):
    """
    each player shares same policy network. we have a separate value network.
    all are MLPs, no CNNs yet.
    """
    def __init__(
        self,
        feature_dim: int,
        last_layer_dim_pi: int
    ):
        self.feature_dim = feature_dim
        self.latent_dim_pi = last_layer_dim_pi
        self.latent_dim_vf = 1
        super().__init__()

        print(
            'MySharedNetwork1', 'feature_dim', feature_dim, 'last_layer_dim_pi',
            last_layer_dim_pi)

        self.policy_net = torch.nn.Sequential(
            torch.nn.Linear(in_features=feature_dim // 3, out_features=128, bias=True),
            torch.nn.Tanh(),
            torch.nn.Linear(in_features=128, out_features=64, bias=True),
            torch.nn.Tanh(),
            torch.nn.Linear(in_features=64, out_features=last_layer_dim_pi // 3, bias=True)
        )
        self.value_net = torch.nn.Sequential(
            torch.nn.Linear(in_features=feature_dim, out_features=256, bias=True),
            torch.nn.Tanh(),
            torch.nn.Linear(in_features=256, out_features=64, bias=True),
            torch.nn.Tanh(),
            torch.nn.Linear(in_features=64, out_features=1, bias=True)
        )

    def forward(self, features: torch.Tensor) -> Tuple[torch.Tensor, torch.Tensor]:
        return self.forward_actor(features), self.forward_critic(features)

    def forward_actor(self, features: torch.Tensor) -> torch.Tensor:
        actions_l = []
        chunks = features.split(features.size(-1) // 3, dim=-1)
        for player_idx in range(3):
            chunk = chunks[player_idx]
            _actions = self.policy_net(chunk)
            actions_l.append(_actions)
        actions_t = torch.cat(actions_l, dim=-1)
        return actions_t

    def forward_critic(self, features: torch.Tensor) -> torch.Tensor:
        return self.value_net(features)


class MySharedNetworkBoth(nn.Module):
    """
    each player shares same policy network. we have a separate value network.
    all are MLPs, no CNNs yet.
    """
    def __init__(
        self,
        feature_dim: int,
        last_layer_dim_pi: int
    ):
        self.feature_dim = feature_dim
        self.latent_dim_pi = last_layer_dim_pi
        self.latent_dim_vf = 1
        super().__init__()

        print(
            'MySharedNetwork1', 'feature_dim', feature_dim, 'last_layer_dim_pi',
            last_layer_dim_pi)

        self.policy_net = torch.nn.Sequential(
            torch.nn.Linear(in_features=feature_dim // 3, out_features=128, bias=True),
            torch.nn.Tanh(),
            torch.nn.Linear(in_features=128, out_features=64, bias=True),
            torch.nn.Tanh(),
            torch.nn.Linear(in_features=64, out_features=last_layer_dim_pi // 3, bias=True)
        )
        self.value_net = torch.nn.Sequential(
            torch.nn.Linear(in_features=feature_dim // 3, out_features=128, bias=True),
            torch.nn.Tanh(),
            torch.nn.Linear(in_features=128, out_features=32, bias=True),
            torch.nn.Tanh(),
        )
        self.value_fuse_net = torch.nn.Linear(in_features=32 * 3, out_features=1, bias=True)

    def forward(self, features: torch.Tensor) -> Tuple[torch.Tensor, torch.Tensor]:
        return self.forward_actor(features), self.forward_critic(features)

    def forward_actor(self, features: torch.Tensor) -> torch.Tensor:
        actions_l = []
        chunks = features.split(features.size(-1) // 3, dim=-1)
        for player_idx in range(3):
            chunk = chunks[player_idx]
            _actions = self.policy_net(chunk)
            actions_l.append(_actions)
        actions_t = torch.cat(actions_l, dim=-1)
        return actions_t

    def forward_critic(self, features: torch.Tensor) -> torch.Tensor:
        values_l = []
        chunks = features.split(features.size(-1) // 3, dim=-1)
        for player_idx in range(3):
            chunk = chunks[player_idx]
            _value = self.value_net(chunk)
            values_l.append(_value)
        values_t = torch.cat(values_l, dim=-1)
        return self.value_fuse_net(values_t)


class MyPolicy(ActorCriticPolicy):
    def __init__(
        self,
        observation_space: gym.spaces.Space,
        action_space: gym.spaces.Space,
        lr_schedule: Callable[[float], float],
        net_arch: Optional[Union[List[int], Dict[str, List[int]]]] = None,
        activation_fn: Type[nn.Module] = nn.Tanh,
        PolicyNetwork: Optional[Type[nn.Module]] = None,
        *args: List[Any],
        **kwargs: Dict[str, Any],
    ):
        print('MyPolicy.__init__()')
        # Disable orthogonal initialization
        self.ortho_init = False
        self.action_space: gym.spaces.Space = action_space
        self.PolicyNetwork = PolicyNetwork
        print('policynetwork', self.PolicyNetwork, PolicyNetwork)
        super().__init__(
            observation_space,
            action_space,
            lr_schedule,
            net_arch,
            activation_fn,
            # Pass remaining arguments to base class
            *args,  # type: ignore
            **kwargs,  # type: ignore
        )

    def _build_mlp_extractor(self) -> None:
        print('action_space.shape', self.action_space.shape)  # type: ignore
        print('self.PolicyNetwork', self.PolicyNetwork)
        self.mlp_extractor = self.PolicyNetwork(
            feature_dim=self.features_dim,  # type: ignore
            last_layer_dim_pi=self.action_space.nvec.sum())  # type: ignore

    # def _passthru(self, *args: tuple[Any]) -> tuple[Any]:
    #     return args  # type: ignore

    def _build(self, lr_schedule: Schedule) -> None:
        super()._build(lr_schedule)
        self.value_net = nn.Identity()  # type: ignore
        self.action_net = nn.Identity()  # type: ignore
