using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;

public class CutsceneSkipper : MonoBehaviour
{
    [Header("Timeline")]
    public PlayableDirector director;   // có cũng được, không có cũng không sao

    [Header("Target Scene")]
    public string nextSceneName = "Defender Of Tomorrow";

    [Header("Input")]
    public KeyCode skipKey = KeyCode.Escape; // ấn phím này cũng skip

    void Update()
    {
        if (Input.GetKeyDown(skipKey))
        {
            Skip();
        }
    }

    public void Skip()
    {
        Debug.Log("[CutsceneSkipper] Skip() called");   // <-- log để biết có bấm được không

        // Nếu có timeline thì cho nó nhảy tới cuối (optional)
        if (director != null)
        {
            director.time = director.duration;
            director.Evaluate();
            director.Stop();
        }

        // Thử load scene
        Debug.Log($"[CutsceneSkipper] Loading scene: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }
}
