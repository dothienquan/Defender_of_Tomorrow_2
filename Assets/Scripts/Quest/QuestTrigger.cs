using UnityEngine;

public class QuestTrigger : MonoBehaviour
{
    [SerializeField] private PassiveQuest quest;   // kéo tay trong Inspector cho chắc

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
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("QuestTrigger: OnTriggerEnter2D với " + other.name);

        if (!other.CompareTag("Player")) return;

        if (quest != null)
        {
            quest.OnAction1Done();  
            Debug.Log("QuestTrigger: ĐÃ gọi quest.OnAction1Done()");
        }
    }

}
