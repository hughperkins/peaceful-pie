using UnityEngine;

public class QuitHandler : MonoBehaviour
{
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape))
        {
            this.Quit();
        }
    }

    void Quit()
    {
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit(0);
    }
}
