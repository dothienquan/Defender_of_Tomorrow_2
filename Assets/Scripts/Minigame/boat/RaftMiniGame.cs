using UnityEngine;
using UnityEngine.UI;

public class RaftMiniGame : MonoBehaviour
{
    [Header("Progress Settings")]
    public int clicksToBuild = 100;
    public int currentClicks = 0;

    [Header("UI")]
    public Image raftProgressImage;   // sprite thuyền dùng làm progress bar

    [Header("Raft Spawn")]
    public GameObject raftPrefab;
    public Transform spawnPoint;

    private bool raftDone = false;

    private void OnEnable()
    {
        currentClicks = 0;

        if (raftProgressImage != null)
            raftProgressImage.fillAmount = 0f;
    }

    public void OnClickBuildButton()
    {
        if (raftDone) return;

        currentClicks++;

        float progress = (float)currentClicks / clicksToBuild;

        // cập nhật thanh tiến độ (fill từ 0 → 1)
        if (raftProgressImage != null)
            raftProgressImage.fillAmount = progress;

        if (currentClicks >= clicksToBuild)
        {
            BuildRaft();
        }
    }

    private void BuildRaft()
    {
        raftDone = true;

        if (raftPrefab != null && spawnPoint != null)
            Instantiate(raftPrefab, spawnPoint.position, spawnPoint.rotation);

        gameObject.SetActive(false);

        Debug.Log("Bè đã hoàn thành!");
    }
}
