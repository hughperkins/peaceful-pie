from typing import Any, Optional, Type
import datetime
import time
import requests
from chili import init_dataclass


URL_TEMPL = "http://localhost:{port}/jsonrpc"


class CSException(Exception):
    pass


class UnityComms:
    def __init__(self, RLResultType: Type) -> None:
        self.session = requests.Session()
        self.jsonrpc_id = 0

    def _rpc_request_dict(self, method: str, params: dict[str, Any]) -> dict[str, Any]:
        res = {"method": method, "params": params, "jsonrpc": "2.0", "id": self.jsonrpc_id}
        self.jsonrpc_id += 1
        return res

    def rpc_call(
        self, unity_port: int, method: str, params_dict: dict[str, Any], ResultClass: Optional[Type]
    ) -> Any:
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
                    print(err_data["ClassName"])
                    print(err_data["Message"])
                    print(err_data["StackTraceString"].replace("\\n", "\n"))
                    raise CSException(err_data["Message"])
                if ResultClass is None:
                    return None
                response = init_dataclass(res_d["result"], ResultClass)
            except requests.exceptions.ConnectionError:
                print("requests.exceptions.ConnectionError => ignoring, retrying")
                time.sleep(0.1)
            except CSException as e:
                raise e
            except Exception as e:
                print("payload", payload)
                print("res", res)
                print("res.content", res.content)
                with open('/tmp/rl1.log', 'w') as f:
                    datetime_str = datetime.datetime.now().strftime("%Y%m%d %H%M%S")
                    f.write(f"{datetime_str}: payload {payload}\n")
                    f.write(f"{datetime_str}: res {res}\n")
                    f.write(f"{datetime_str}: res.content {str(res.content)}\n")
                    f.write(f"{datetime_str}: e {e}\n")
        return response
