using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TriggerZone : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public MirrorPuzzleManager puzzle;
    public string playerTag = "Player";

    [Header("Interaction Settings")]
    [SerializeField] private bool requireInteraction = true; // Cần nhấn F để bắt đầu
    [SerializeField] private KeyCode interactionKey = KeyCode.F;
    [SerializeField] private InteractionUI interactionUI; // UI hiển thị hint [F] Tương tác
    [SerializeField] private string interactionText = "Bắt đầu Puzzle";

    private int playerInsideCount = 0;
    private bool puzzleStarted = false;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c) c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInsideCount++;
        
        if (playerInsideCount == 1)
        {
            if (requireInteraction)
            {
                // Chỉ hiển thị UI hint nếu puzzle chưa được start
                if (!puzzleStarted && interactionUI != null)
                {
                    interactionUI.Show(interactionText);
                }
            }
            else
            {
                // Tự động start puzzle như cũ
                StartPuzzle();
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInsideCount = Mathf.Max(0, playerInsideCount - 1);
        
        if (playerInsideCount == 0)
        {
            // Ẩn UI hint
            if (interactionUI != null)
            {
                interactionUI.Hide();
            }
            
            // Reset puzzle nếu đã start
            if (puzzleStarted && puzzle != null)
            {
                puzzle.ResetPuzzle();
                puzzleStarted = false; // Reset flag để UI có thể hiển thị lại khi quay lại
            }
        }
    }

    void Update()
    {
        // Chỉ xử lý interaction nếu player đang trong trigger và puzzle chưa start
        if (playerInsideCount > 0 && requireInteraction && !puzzleStarted)
        {
            if (Input.GetKeyDown(interactionKey))
            {
                Debug.Log($"[TriggerZone] Nhấn {interactionKey} - Bắt đầu puzzle");
                StartPuzzle();
            }
        }
    }

    private void StartPuzzle()
    {
        if (puzzle == null) return;

        puzzleStarted = true;
        puzzle.StartPuzzle();

        // Ẩn UI hint sau khi start
        if (interactionUI != null)
        {
            interactionUI.Hide();
            Debug.Log("[TriggerZone] Đã ẩn InteractionUI sau khi start puzzle");
        }
        else
        {
            Debug.LogWarning("[TriggerZone] InteractionUI chưa được gán! Vui lòng gán InteractionUI vào field 'Interaction UI' trong Inspector.");
        }
    }
}
