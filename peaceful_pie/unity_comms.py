import atexit
from contextlib import contextmanager
import dataclasses
from typing import Any, Generator, Optional, Type
import datetime
import time
import subprocess
import requests
import chili


URL_TEMPL = "http://{hostname}:{port}/jsonrpc"


class CSException(Exception):
    pass


class UnityCommsFn:
    def __init__(self, unity_comms: 'UnityComms', method_name: str):
        self.unity_comms = unity_comms
        self.method_name = method_name

    def __call__(self, ResultClass: Optional[Type] = None, retry: bool = True, **kwargs: Any) -> Any:
        return self.unity_comms.rpc_call(
            method=self.method_name, retry=retry, params_dict=kwargs, ResultClass=ResultClass)


class UnityComms:
    def __init__(
        self,
        port: int,
        server_executable_path: Optional[str] = None,
        logfile: Optional[str] = None,
        hostname: str = "localhost"
    ) -> None:
        """
        :param port: int The port that Unity will run on. Always mandatory. If server_executable_path is provided, we
            will start the server with commandline `--port {port}`
        :param server_executable_path: Optional[str] Path to dedicatd server executable to run
        :param logfile: Optional[str] Logfile to write to. Optional
        :param hostname: str  If providing server_executable_path, must be 'localhost', otherwise the hostname where
            the Unity process is running. Reminder that all network communications are insecure, so best to set this
            to localhost. (For secure network communciations you could use e.g. ssh tunnels)
        """
        self.server_executable_path = server_executable_path
        if server_executable_path is not None:
            assert hostname == "localhost", "Must use hostname localhost if passing in server_executable_path"
        self.hostname = hostname
        self.port = port
        self.session = requests.Session()
        self.jsonrpc_id = 0
        self.logfile = logfile

        self.server_process: Optional[subprocess.Popen] = None

        if server_executable_path is not None:
            atexit.register(self._kill_server)
            self._start_server()

    def _start_server(self) -> None:
        assert self.server_executable_path is not None
        cmd_line = [self.server_executable_path, '--port', str(self.port)]
        print(cmd_line)
        self.server_process = subprocess.Popen(cmd_line)

    def _kill_server(self) -> None:
        if self.server_process is not None:
            print('killing server process')
            self.server_process.kill()

    def _rpc_request_dict(self, method: str, params: dict[str, Any]) -> dict[str, Any]:
        res = {"method": method, "params": params, "jsonrpc": "2.0", "id": self.jsonrpc_id}
        self.jsonrpc_id += 1
        return res

    @contextmanager
    def blocking_listen(self) -> Generator:
        """
        Uses blocking listens for commands from the python side. Note that on a dedicated unity server, this is
        automatic: no need to call this. This only has an affect when running against a non-dedicated Unity, typically
        the Unity Editor.

        To avoid blocking leaving the Editor blocked after the script exits, this should be used from a `with` block,
        like:

        with unity_comms.blocking_listen():
            # do other stuff here

        (This means we automatically attempt to remove the blocking listen from the Unity Editor, at the end)
        """
        self.rpc_call("setBlockingListen", {"blocking": True})
        try:
            yield None
        finally:
            self.rpc_call("setBlockingListen", {"blocking": False}, retry=False)

    def set_autosimulation(self, auto_simulation: bool) -> None:
        """
        You should call this to turn off autosimulation prior to running any reinforcement learning
        algorithm. Otherwise your environment risks stepping in between your reinforcement learning
        steps :P
        """
        self.rpc_call("setAutosimulation", {"autosimulation": auto_simulation})

    def get_autosimulation(self) -> bool:
        return self.rpc_call("getAutosimulation")

    def __getattr__(self, method_name: str) -> UnityCommsFn:
        return UnityCommsFn(unity_comms=self, method_name=method_name)

    def __getitem__(self, method_name: str) -> UnityCommsFn:
        return UnityCommsFn(unity_comms=self, method_name=method_name)

    def rpc_call(
        self,
        method: str,
        params_dict: Optional[dict[str, Any]] = None,
        ResultClass: Optional[Type] = None,
        retry: bool = True,
        **kwargs: dict[str, Any]
    ) -> Any:
        """
        :param unity_port: int The Port that our Unity application is listening on
        :param method: str The json rpc method we want to call. Usually this is the name of the method
        :param params_dict: Optional[dict[str, Any]] Any parameters we want to pass into the method,
            keyed by parameter name
            Can provide structured data using dataclasses
        :param ResultClass: Optional[Type] If not None, then the returned json dict will be converted
            into this type, using Chili. Should be a dataclass. If None, then the raw result dict will
            be returned instead.
        :param **kwargs: dict[str, Any]  You can also simply pass in parameters by name
        """
        params_dict = params_dict if params_dict else {}
        params_dict.update(kwargs)
        new_dict = {}
        for k, v in params_dict.items():
            if dataclasses.is_dataclass(v):
                v = chili.asdict(v)
            new_dict[k] = v
        params_dict = new_dict
        payload = self._rpc_request_dict(method, params_dict)
        url = URL_TEMPL.format(port=self.port, hostname=self.hostname)
        response = None
        while response is None:
            try:
                res = self.session.post(url, json=payload)
                if res.content is None:
                    print("content is None => skipping")
                    continue
                if res.content == "".encode("utf-8"):
                    print("no json content detected => skipping")
                    continue
                if res.content.decode("utf-8").strip() == "":
                    print("stripped content is empty string => skipping")
                    continue
                res_d = res.json()
                if "error" in res_d:
                    print("res_d", res_d)
                    err_data = res_d["error"]["data"]
                    if isinstance(err_data, dict):
                        print(err_data["ClassName"])
                        print(err_data["Message"])
                        print(err_data["StackTraceString"].replace("\\n", "\n"))
                        raise CSException(err_data["Message"])
                    else:
                        raise CSException(err_data)
                if ResultClass is None:
                    return res_d["result"]
                response = chili.init_dataclass(res_d["result"], ResultClass)
            except requests.exceptions.ConnectionError:
                if retry:
                    print("requests.exceptions.ConnectionError => ignoring, retrying")
                    time.sleep(0.1)
                else:
                    return
            except CSException as e:
                print("payload", payload)
                print("res", res)
                raise e
            except Exception as e:
                print("payload", payload)
                print("res", res)
                print("res.content", res.content)
                print("e", e)
                if self.logfile is not None:
                    with open(self.logfile, 'a') as f:
                        datetime_str = datetime.datetime.now().strftime("%Y%m%d %H%M%S")
                        f.write(f"{datetime_str}: payload {payload}\n")
                        f.write(f"{datetime_str}: res {res}\n")
                        f.write(f"{datetime_str}: res.content {str(res.content)}\n")
                        f.write(f"{datetime_str}: e {e}\n")
                time.sleep(0.1)
        return response
