using UnityEngine;
using UnityEngine.Playables;

public class CutsceneSkip : MonoBehaviour
{
    public PlayableDirector director;      // Timeline của cutscene
    public GameObject cutsceneUI;          // UI đang hiện khi cutscene
    public PlayerController player;        // player để bật điều khiển lại

    public void Skip()
    {
        // tua tới cuối timeline
        director.time = director.duration;
        director.Evaluate();
        director.Stop();

        // tắt UI cutscene
        if (cutsceneUI != null) cutsceneUI.SetActive(false);

        // bật điều khiển lại
        if (player != null) player.EnableControls();
    }
}