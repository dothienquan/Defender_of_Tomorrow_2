using UnityEngine;
using TMPro;

public class NPCDialogue : MonoBehaviour
{
    [Header("NPC Info")]
    public string npcName = "Lillia";

    [Header("Dialogue")]
    public DialogueObject firstDialogue;     // thoại lần đầu
    public DialogueObject repeatDialogue;    // thoại từ lần 2 trở đi

    [Header("UI References")]
    public DialogueUI dialogueUI;            // kéo DialoguePanel vào
    public TextMeshProUGUI fHint;            // kéo Text "[F] ..." vào

    [Header("Input")]
    public KeyCode key = KeyCode.F;

    bool playerInRange;
    bool hasTalked = false;                  // mỗi NPC có biến riêng

    void Start()
    {
        if (fHint != null)
            fHint.gameObject.SetActive(false);

        if (dialogueUI == null)
            Debug.LogError($"[{name}] dialogueUI chưa được gán!");
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
            fHint.text = $"[{key}] {npcName}";
            fHint.gameObject.SetActive(true);
        }

        // Nếu không có dialogue nào → nhấn F cũng không làm gì, nhưng có log
        if (currentDialogue == null)
        {
            // Chỉ log 1 lần cho NPC này
            return;
        }

        // ---- NHẤN F → MỞ THOẠI ----
        if (Input.GetKeyDown(key))
        {
            if (fHint != null) fHint.gameObject.SetActive(false);

            if (!hasTalked)
                hasTalked = true; // từ lần sau trở đi sẽ dùng repeatDialogue (nếu có)

            dialogueUI.Show(currentDialogue, npcName);
        }
    }

    DialogueObject GetCurrentDialogue()
    {
        // Ưu tiên: lần đầu → first, về sau → repeat
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
            playerInRange = true;
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
