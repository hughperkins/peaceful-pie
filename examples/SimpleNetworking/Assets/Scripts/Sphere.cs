using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AustinHarris.JsonRpc;

public class MyVector3 {
    public float x;
    public float y;
    public float z;
    public MyVector3(Vector3 v) {
        this.x = v.x;
        this.y = v.y;
        this.z = v.z;
    }
    public Vector3 ToVector3() {
        return new Vector3(x, y, z);
    }
}

public class Sphere : MonoBehaviour
{
    class Rpc : JsonRpcService {
        Sphere sphere;
        public Rpc(Sphere sphere) {
            this.sphere = sphere;
        }

        [JsonRpcMethod]
        void say(string message) {
            Debug.Log($"you sent {message}");
        }

        [JsonRpcMethod]
        float getHeight() {
            return sphere.transform.position.y;
        }

        [JsonRpcMethod]
        MyVector3 getPosition() {
            return new MyVector3(sphere.transform.position);
        }

        [JsonRpcMethod]
        void translate(MyVector3 translate) {
            sphere.transform.position += translate.ToVector3();
        }
    }

    Rpc rpc;

    void Awake() {
        rpc = new Rpc(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
