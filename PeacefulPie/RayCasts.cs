using System.Collections.Generic;
using UnityEngine;

public class RayCasts: MonoBehaviour {
    [Tooltip("Consider the rays form a low-resolution image. This is the x-resolution of that image.")]
    public int XResolution = 5;
    [Tooltip("Consider the rays form a low-resolution image. This is the y-resolution of that image.")]
    public int YResolution = 5;
    [Tooltip("What is the total horizontal angle subtended by the rays? (60 is good default)")]
    public int XTotalAngle = 60;
    [Tooltip("What is the total vertical angle subtended by the rays? (60 is good default)")]
    public int YTotalAngle = 60;

    class Ray {
        public Vector3 direction;
        public Ray(Vector3 direction) {
            this.direction = direction;
        }
    }

    List<List<Ray>> rays = new List<List<Ray>>();  // [x][y]

    void Start()
    {
        for(int x_idx = 0; x_idx < XResolution; x_idx++) {
            float x_angle = XTotalAngle / (XResolution - 1) * x_idx - XTotalAngle / 2;
            rays.Add(new List<Ray>());
            for(int y_idx = 0; y_idx < YResolution; y_idx++) {
                float y_angle = YTotalAngle / (YResolution - 1) * y_idx - YTotalAngle / 2;
                Vector3 vec = Quaternion.Euler(x_angle, y_angle, 0) * Vector3.forward;
                rays[x_idx].Add(new Ray(vec));
            }
        }
        Debug.Log("added rays");
    }

    void Update()
    {
        for(int x_idx = 0; x_idx < rays.Count; x_idx++) {
            var _rays = rays[x_idx];
            for(int y_idx = 0; y_idx < _rays.Count; y_idx++) {
                var ray = _rays[y_idx];
                Debug.DrawRay(transform.position, transform.TransformDirection(ray.direction) * 10);
            }
        }
    }

    public void RunRays(out List<List<float>> rayDistances, out List<List<int>> rayHitObjectTypes) {
        rayDistances = new List<List<float>>();
        rayHitObjectTypes = new List<List<int>>();
        for(int x_idx = 0; x_idx < rays.Count; x_idx++) {
            rayDistances.Add(new List<float>());
            rayHitObjectTypes.Add(new List<int>());
            var _rays = rays[x_idx];
            for(int y_idx = 0; y_idx < _rays.Count; y_idx++) {
                var ray = _rays[y_idx];
                RaycastHit hit;
                if(Physics.Raycast(transform.position, transform.TransformDirection(ray.direction), out hit)) {
                    rayDistances[x_idx].Add(hit.distance);
                    rayHitObjectTypes[x_idx].Add(hit.collider.gameObject.layer);
                } else {
                    rayDistances[x_idx].Add(-1);
                    rayHitObjectTypes[x_idx].Add(-1);
                }
            }
        }
    }
}
