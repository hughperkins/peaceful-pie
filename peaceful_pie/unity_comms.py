from typing import Any, Optional, Type
import datetime
import time
import requests
from chili import init_dataclass


URL_TEMPL = "http://localhost:{port}/jsonrpc"


class CSException(Exception):
    pass


class UnityComms:
    def __init__(self, logfile: Optional[str] = None) -> None:
        self.session = requests.Session()
        self.jsonrpc_id = 0
        self.logfile = logfile

    def _rpc_request_dict(self, method: str, params: dict[str, Any]) -> dict[str, Any]:
        res = {"method": method, "params": params, "jsonrpc": "2.0", "id": self.jsonrpc_id}
        self.jsonrpc_id += 1
        return res

    def set_blocking_listen(self, unity_port: int, blocking: bool) -> None:
        self.rpc_call(unity_port, "setBlockingListen", {"blocking": blocking}, retry=False)

    def rpc_call(
        self,
        unity_port: int,
        method: str,
        params_dict: Optional[dict[str, Any]] = None,
        ResultClass: Optional[Type] = None,
        retry: bool = True
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
        """
        params_dict = params_dict if params_dict else {}
        payload = self._rpc_request_dict(method, params_dict)
        url = URL_TEMPL.format(port=unity_port)
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
                response = init_dataclass(res_d["result"], ResultClass)
            except requests.exceptions.ConnectionError:
                if retry:
                    print("requests.exceptions.ConnectionError => ignoring, retrying")
                    time.sleep(0.1)
                else:
                    return
            except CSException as e:
                raise e
            except Exception as e:
                print("payload", payload)
                print("res", res)
                print("res.content", res.content)
                if self.logfile is not None:
                    with open(self.logfile, 'a') as f:
                        datetime_str = datetime.datetime.now().strftime("%Y%m%d %H%M%S")
                        f.write(f"{datetime_str}: payload {payload}\n")
                        f.write(f"{datetime_str}: res {res}\n")
                        f.write(f"{datetime_str}: res.content {str(res.content)}\n")
                        f.write(f"{datetime_str}: e {e}\n")
                time.sleep(0.1)
        return response
