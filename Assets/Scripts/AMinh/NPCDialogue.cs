using UnityEngine;
using TMPro;
using System.IO; // Thư viện cần thiết để kiểm tra file save

public class NPCDialogue : MonoBehaviour
{
    [Header("NPC Info")]
    public string npcName = "Lillia";
    [Tooltip("Avatar/ảnh đại diện của NPC (hiển thị trong dialogue)")]
    public Sprite npcAvatar;

    [Header("Dialogue")]
    public DialogueObject firstDialogue;     // thoại lần đầu
    public DialogueObject repeatDialogue;    // thoại từ lần 2 trở đi
    public DialogueObject autoTriggerDialogue; // Dialogue tự động kích hoạt khi trigger (optional)

    [Header("UI References")]
    public DialogueUI dialogueUI;            // kéo DialoguePanel vào
    public TextMeshProUGUI fHint;            // kéo Text "[F] ..." vào

    [Header("Input")]
    public KeyCode key = KeyCode.F;

    [Header("Auto Trigger Settings")]
    [Tooltip("Tự động kích hoạt dialogue khi player chạm vào trigger")]
    public bool enableAutoTrigger = false;
    [Tooltip("Dialogue chỉ xuất hiện 1 lần (lưu vào PlayerPrefs)")]
    public bool showOnlyOnce = true;
    [Tooltip("Unique ID cho trigger này (dùng để lưu trạng thái đã hiển thị)")]
    public string triggerID = "";

    bool playerInRange;
    bool hasTalked = false;                  // mỗi NPC có biến riêng
    private string autoTriggerKey;           // Key để lưu trạng thái đã hiển thị

    void Start()
    {
        if (fHint != null)
            fHint.gameObject.SetActive(false);

        if (dialogueUI == null)
            Debug.LogError($"[{name}] dialogueUI chưa được gán!");

        // Tạo unique key cho auto trigger
        if (enableAutoTrigger)
        {
            // Sử dụng triggerID nếu có, nếu không thì dùng GameObject name
            string id = !string.IsNullOrEmpty(triggerID) ? triggerID : gameObject.name;
            autoTriggerKey = $"NPCDialogue_AutoTrigger_{id}";

            // --- QUAN TRỌNG: Kiểm tra và Reset nếu là New Game ---
            CheckAndResetStateIfNewGame();
        }
    }

    /// <summary>
    /// Kiểm tra xem file save có tồn tại không. 
    /// Nếu không có (New Game/Delete Data) mà PlayerPrefs vẫn còn lưu trạng thái cũ -> Reset ngay.
    /// </summary>
    private void CheckAndResetStateIfNewGame()
    {
        // Đường dẫn file save (phải khớp với SaveController)
        string savePath = Path.Combine(Application.persistentDataPath, "saveData.json");

        // Nếu File Save KHÔNG tồn tại (đã bị xóa hoặc chơi lần đầu)
        if (!File.Exists(savePath))
        {
            // Nhưng PlayerPrefs lại CÓ key này (tàn dư của lần chơi trước)
            if (PlayerPrefs.HasKey(autoTriggerKey))
            {
                // Reset trạng thái auto-trigger
                ResetAutoTriggerState();
                
                // Reset biến hasTalked để NPC nói lại câu thoại đầu tiên
                hasTalked = false; 
                
                Debug.Log($"[NPCDialogue] Phát hiện New Game (Không thấy file Save). Đã reset trạng thái cho '{gameObject.name}'");
            }
        }
    }

    void Update()
    {
        if (!playerInRange) return;
        if (dialogueUI == null) return;

        // ---- CHỌN DIALOGUE CHO LẦN NÀY ----
        DialogueObject currentDialogue = GetCurrentDialogue();

        // Luôn hiện F hint khi lại gần, dù có dialogue hay không
        if (!dialogueUI.gameObject.activeSelf && fHint != null)
        {
            fHint.text = $"{npcName}";
            fHint.gameObject.SetActive(true);
        }

        // Nếu không có dialogue nào -> return
        if (currentDialogue == null) return;

        // ---- NHẤN F -> MỞ THOẠI ----
        if (Input.GetKeyDown(key))
        {
            if (fHint != null) fHint.gameObject.SetActive(false);

            if (!hasTalked)
                hasTalked = true; // từ lần sau trở đi sẽ dùng repeatDialogue (nếu có)

            dialogueUI.Show(currentDialogue, npcName, npcAvatar);
        }
    }

    DialogueObject GetCurrentDialogue()
    {
        // Ưu tiên: lần đầu -> first, về sau -> repeat
        if (!hasTalked)
        {
            if (firstDialogue != null) return firstDialogue;
            if (repeatDialogue != null) return repeatDialogue; // fallback nếu quên gán first
        }
        else
        {
            if (repeatDialogue != null) return repeatDialogue;
            if (firstDialogue != null) return firstDialogue; // fallback nếu không có repeat
        }

        Debug.LogWarning($"[{name}] Không có DialogueObject nào được gán (first/repeat đều null).");
        return null;
    }

    // 2D trigger
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            
            // Tự động kích hoạt dialogue nếu được bật
            if (enableAutoTrigger)
            {
                TryAutoTriggerDialogue();
            }
        }
    }
    
    /// <summary>
    /// Thử tự động kích hoạt dialogue (chỉ 1 lần nếu showOnlyOnce = true)
    /// </summary>
    private void TryAutoTriggerDialogue()
    {
        if (dialogueUI == null)
        {
            Debug.LogWarning($"[NPCDialogue] Cannot auto-trigger: dialogueUI is null!");
            return;
        }

        // Kiểm tra xem dialogue đang active không (tránh trigger nhiều lần)
        if (dialogueUI.gameObject.activeSelf) return;

        // Kiểm tra xem dialogue đã được hiển thị chưa (nếu showOnlyOnce = true)
        if (showOnlyOnce)
        {
            bool hasShown = PlayerPrefs.GetInt(autoTriggerKey, 0) == 1;
            if (hasShown)
            {
                // Đã hiện rồi thì thôi
                return;
            }
        }

        // Chọn dialogue để hiển thị
        DialogueObject dialogueToShow = null;
        
        // Ưu tiên autoTriggerDialogue, nếu không có thì dùng firstDialogue
        if (autoTriggerDialogue != null)
            dialogueToShow = autoTriggerDialogue;
        else if (firstDialogue != null)
            dialogueToShow = firstDialogue;
        else
        {
            Debug.LogWarning($"[NPCDialogue] No dialogue assigned for auto-trigger on '{gameObject.name}'!");
            return;
        }

        // Hiển thị dialogue
        if (fHint != null) fHint.gameObject.SetActive(false);
        
        dialogueUI.Show(dialogueToShow, npcName, npcAvatar);
        
        // Đánh dấu đã hiển thị (nếu showOnlyOnce = true)
        if (showOnlyOnce)
        {
            PlayerPrefs.SetInt(autoTriggerKey, 1);
            PlayerPrefs.Save();
            Debug.Log($"[NPCDialogue] Auto-trigger dialogue shown and marked as shown for '{gameObject.name}'.");
        }
        
        // Đánh dấu đã nói chuyện (để lần sau dùng repeatDialogue nếu nhấn F)
        if (!hasTalked)
        {
            hasTalked = true;
        }
    }
    
    /// <summary>
    /// Reset trạng thái auto-trigger (dùng khi New Game)
    /// </summary>
    public void ResetAutoTriggerState()
    {
        if (!string.IsNullOrEmpty(autoTriggerKey))
        {
            PlayerPrefs.DeleteKey(autoTriggerKey);
            PlayerPrefs.Save();
            Debug.Log($"[NPCDialogue] Reset auto-trigger state for '{gameObject.name}'.");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (fHint != null) fHint.gameObject.SetActive(false);
        }
    }
}