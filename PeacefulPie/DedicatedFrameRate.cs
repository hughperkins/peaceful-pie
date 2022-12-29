using UnityEngine;

class DedicatedFrameRate: MonoBehaviour {
	public bool isDedicated()
	{
		return Screen.currentResolution.refreshRate == 0;
	}

	void Start()
	{
		Debug.Log("is dedicated " + isDedicated());
		if (isDedicated())
		{
			Application.targetFrameRate = 10;
		}
	}

    void Update() {
        // Debug.Log($"delta time {Time.deltaTime}");
    }

    void FixedUpdate() {
        // Debug.Log($"fixed delta time {Time.fixedDeltaTime}");
    }
}
