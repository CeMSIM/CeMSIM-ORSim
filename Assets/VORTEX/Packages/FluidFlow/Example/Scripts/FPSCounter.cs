using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public UnityEngine.UI.Text fps;

    void Update()
    {
        fps.text = "fps: " + (int)(1f / Time.smoothDeltaTime);
    }
}
