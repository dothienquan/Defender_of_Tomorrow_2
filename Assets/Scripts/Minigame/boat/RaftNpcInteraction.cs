using UnityEngine;

public class RaftNpcInteraction : MonoBehaviour
{
    [Header("Mini Game UI")]
    public GameObject miniGamePanel;          // panel mini game
    public KeyCode interactKey = KeyCode.F;   // phím tương tác

    private RaftMaterialCollector playerMaterials;
    private bool playerInRange = false;

    private void Start()
    {
        if (miniGamePanel != null)
            miniGamePanel.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var collector = other.GetComponent<RaftMaterialCollector>();
        if (collector != null)
        {
            playerMaterials = collector;
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var collector = other.GetComponent<RaftMaterialCollector>();
        if (collector != null && collector == playerMaterials)
        {
            playerMaterials = null;
            playerInRange = false;
        }
    }

    private void Update()
    {
        if (!playerInRange || playerMaterials == null) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (playerMaterials.HasEnoughMaterials)
            {
                OpenMiniGame();
            }
            else
            {
                Debug.Log("Chưa đủ nguyên liệu để làm bè!");
                //  gọi UI/Dialogue chỗ này
            }
        }
    }

    private void OpenMiniGame()
    {
        if (miniGamePanel != null)
            miniGamePanel.SetActive(true);

        // nếu có hệ thống khóa input player thì gọi ở đây
        // PlayerController.Instance.enabled = false; (ví dụ)
    }

    public void CloseMiniGame()
    {
        if (miniGamePanel != null)
            miniGamePanel.SetActive(false);
    }
}
