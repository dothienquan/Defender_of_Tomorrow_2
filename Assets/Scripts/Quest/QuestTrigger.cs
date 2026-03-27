using UnityEngine;

public class QuestTrigger : MonoBehaviour
{
    [SerializeField] private PassiveQuest quest;   // kéo tay trong Inspector cho chắc
    [SerializeField] private int actionNumber = 1;  // Số action cần trigger (1-10)
    [SerializeField] private bool triggerOnce = true;  // Chỉ trigger một lần
    [SerializeField] private bool triggerOnEnter = true;  // Trigger khi enter (false = trigger khi exit)

    private bool hasTriggered = false;

    private void Awake()
    {
        if (quest == null)
        {
            quest = FindObjectOfType<PassiveQuest>();
            if (quest == null)
            {
                Debug.LogError("QuestTrigger: KHÔNG tìm thấy PassiveQuest trong scene!");
            }
        }

        // Clamp action number to valid range
        actionNumber = Mathf.Clamp(actionNumber, 1, 10);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOnEnter) return;
        TriggerAction(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (triggerOnEnter) return;
        TriggerAction(other);
    }

    private void TriggerAction(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (triggerOnce && hasTriggered) return;

        if (quest != null)
        {
            hasTriggered = true;
            CallActionMethod();
            Debug.Log($"QuestTrigger: ĐÃ gọi quest.OnAction{actionNumber}Done()");
        }
    }

    private void CallActionMethod()
    {
        switch (actionNumber)
        {
            case 1:
                quest.OnAction1Done();
                break;
            case 2:
                quest.OnAction2Done();
                break;
            case 3:
                quest.OnAction3Done();
                break;
            case 4:
                quest.OnAction4Done();
                break;
            case 5:
                quest.OnAction5Done();
                break;
            case 6:
                quest.OnAction6Done();
                break;
            case 7:
                quest.OnAction7Done();
                break;
            case 8:
                quest.OnAction8Done();
                break;
            case 9:
                quest.OnAction9Done();
                break;
            case 10:
                quest.OnAction10Done();
                break;
            default:
                Debug.LogWarning($"QuestTrigger: Action number {actionNumber} không hợp lệ! Phải từ 1-10.");
                break;
        }
    }

    // Method để reset trigger (có thể gọi từ script khác nếu cần)
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
