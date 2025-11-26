using UnityEngine;
using UnityEngine.UI;

public class RaftMiniGame : MonoBehaviour
{
    [Header("Click Settings")]
    public int clicksToBuild = 100;         // cần bấm bao nhiêu lần
    public Text progressText;               // hiển thị % hoặc số (optional)
    public Slider progressSlider;           // thanh tiến độ (optional)

    [Header("Raft Spawn")]
    public GameObject raftPrefab;           // prefab bè
    public Transform raftSpawnPoint;        // chỗ spawn bè
    public bool destroyPanelAfterBuild = true;

    private int currentClicks = 0;
    private bool raftBuilt = false;

    private void OnEnable()
    {
        currentClicks = 0;
        UpdateUI();
    }

    public void OnClickBuildButton()
    {
        if (raftBuilt) return;

        currentClicks++;

        if (currentClicks >= clicksToBuild)
        {
            BuildRaft();
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (progressText != null)
        {
            progressText.text = $"{currentClicks}/{clicksToBuild}";
        }

        if (progressSlider != null)
        {
            progressSlider.maxValue = clicksToBuild;
            progressSlider.value = currentClicks;
        }
    }

    private void BuildRaft()
    {
        raftBuilt = true;

        if (raftPrefab != null && raftSpawnPoint != null)
        {
            Instantiate(raftPrefab, raftSpawnPoint.position, raftSpawnPoint.rotation);
        }

        // tắt panel mini game
        if (destroyPanelAfterBuild)
            gameObject.SetActive(false);

        // nếu muốn báo cho NPC đóng panel
        var npc = FindObjectOfType<RaftNpcInteraction>();
        if (npc != null)
        {
            npc.CloseMiniGame();
        }

        Debug.Log("Bè đã được tạo!");
        // có thể mở cầu/cho phép qua sông ở đây
    }
}
