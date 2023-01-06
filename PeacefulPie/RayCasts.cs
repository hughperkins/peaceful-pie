using System;
using System.Collections.Generic;
using UnityEngine;

public class RayResults {
	public List<List<float>> rayDistances;
	public List<List<int>> rayHitObjectTypes;
	public int NumObjectTypes;
	public RayResults(
	    List<List<float>> rayDistances,
	    List<List<int>> rayHitObjectTypes,
	    int NumObjectTypes
	) {
		this.rayDistances = rayDistances;
		this.rayHitObjectTypes = rayHitObjectTypes;
		this.NumObjectTypes = NumObjectTypes;
	}
}

public class RayCasts : MonoBehaviour {
	[Tooltip("Consider the rays form a low-resolution image. This is the x-resolution of that image.")]
	public int XResolution = 5;
	[Tooltip("Consider the rays form a low-resolution image. This is the y-resolution of that image.")]
	public int YResolution = 5;
	[Tooltip("What is the total horizontal angle subtended by the rays? (60 is good default)")]
	public int XTotalAngle = 60;
	[Tooltip("What is the total vertical angle subtended by the rays? (60 is good default)")]
	public int YTotalAngle = 60;
	[Tooltip("Length of each ray")]
	public int RayLength = 40;
	[Tooltip("Ray radius. 0 means RayCast, >0 is SphereCast")]
	public float RayRadius = 0;
	public bool ShowRaysInEditor = true;
	public bool ShowRaysInPlayer = false;
	[Tooltip("Mandatory field: list here the tags you want to detect. Each will be given it's own output feature plane.")]
	public List<string> DetectableTags = new List<string>();

	class Ray {
		public Vector3 direction;
		public Ray(Vector3 direction) {
			this.direction = direction;
		}
	}

	Vector3 RayDirection(int xIdx, int yIdx) {
		float x_angle = XResolution > 1 ? XTotalAngle / (XResolution - 1.0f) * xIdx - XTotalAngle / 2.0f : 0;
		float y_angle = YResolution > 1 ? YTotalAngle / (YResolution - 1.0f) * yIdx - YTotalAngle / 2.0f : 0;
		Vector3 vec = Quaternion.Euler(y_angle, x_angle, 0) * Vector3.forward;
		return vec;
	}

	void OnDrawGizmosSelected() {
		if(!ShowRaysInEditor) {
			return;
		}
		HashSet<string> tagsSet = new HashSet<string>();
		foreach(string tag in DetectableTags) {
			tagsSet.Add(tag);
		}
		for(int x_idx = 0; x_idx < XResolution; x_idx++) {
			for(int y_idx = 0; y_idx < YResolution; y_idx++) {
				Vector3 vec = RayDirection(x_idx, y_idx);
				RaycastHit hit;
				Gizmos.color = new Color(1, 0, 0, 0.75f);
				if(SingleCast(vec, out hit)) {
					if(tagsSet.Contains(hit.collider.gameObject.tag)) {
						Gizmos.color = new Color(0, 1, 0, 0.75f);
						Gizmos.DrawRay(transform.position, transform.TransformDirection(vec) * hit.distance);
					} else {
						Gizmos.DrawRay(transform.position, transform.TransformDirection(vec) * hit.distance);
					}
				} else {
					Gizmos.DrawRay(transform.position, transform.TransformDirection(vec) * RayLength);
				}
			}
		}
	}

	bool SingleCast(Vector3 direction, out RaycastHit hit) {
		if(RayRadius > 0) {
			return Physics.SphereCast(transform.position, RayRadius, transform.TransformDirection(direction), out hit, RayLength);
		} else {
			return Physics.Raycast(transform.position, transform.TransformDirection(direction), out hit, RayLength);
		}
	}

	void Update() {
		if(!ShowRaysInPlayer) {
			return;
		}
		for(int x_idx = 0; x_idx < XResolution; x_idx++) {
			for(int y_idx = 0; y_idx < YResolution; y_idx++) {
				Vector3 vec = RayDirection(x_idx, y_idx);
				RaycastHit hit;
				if(SingleCast(vec, out hit)) {
					Debug.DrawRay(transform.position, transform.TransformDirection(vec) * hit.distance);
				} else {
					Debug.DrawRay(transform.position, transform.TransformDirection(vec) * RayLength);
				}
			}
		}
	}

	public RayResults GetZerodObservation() {
		// for use if agent is dead, for example
		List<List<float>> rayDistances = new List<List<float>>();
		List<List<int>> rayHitObjectTypes = new List<List<int>>();
		int NumObjectTypes = DetectableTags.Count;
		for(int x_idx = 0; x_idx < XResolution; x_idx++) {
			rayDistances.Add(new List<float>());
			rayHitObjectTypes.Add(new List<int>());
			for(int y_idx = 0; y_idx < YResolution; y_idx++) {
				rayDistances[x_idx].Add(-1);
				rayHitObjectTypes[x_idx].Add(-1);
			}
		}
		return new RayResults(
		    rayDistances,
		    rayHitObjectTypes,
		    NumObjectTypes
		);
	}

	public RayResults GetObservation() {
		List<List<float>> rayDistances = new List<List<float>>();
		List<List<int>> rayHitObjectTypes = new List<List<int>>();
		Dictionary<string, int> tagIdxByName = new Dictionary<string, int>();
		for(int i = 0; i < DetectableTags.Count; i++) {
			tagIdxByName[DetectableTags[i]] = i;
		}
		int NumObjectTypes = DetectableTags.Count;
		for(int x_idx = 0; x_idx < XResolution; x_idx++) {
			rayDistances.Add(new List<float>());
			rayHitObjectTypes.Add(new List<int>());
			for(int y_idx = 0; y_idx < YResolution; y_idx++) {
				Vector3 vec = RayDirection(x_idx, y_idx);
				RaycastHit hit;
				if(SingleCast(vec, out hit)) {
					string tag = hit.collider.gameObject.tag;
					if(tagIdxByName.ContainsKey(tag)) {
						rayDistances[x_idx].Add(hit.distance);
						rayHitObjectTypes[x_idx].Add(tagIdxByName[tag]);
					} else {
						rayDistances[x_idx].Add(-1);
						rayHitObjectTypes[x_idx].Add(-1);
					}
				} else {
					rayDistances[x_idx].Add(-1);
					rayHitObjectTypes[x_idx].Add(-1);
				}
			}
		}
		return new RayResults(
		    rayDistances,
		    rayHitObjectTypes,
		    NumObjectTypes
		);
	}
}
