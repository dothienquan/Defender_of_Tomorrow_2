using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    public DialogueObject dialogue;
    public KeyCode key = KeyCode.F;

    bool playerInRange;
    DialogueUI dialogueUI;

    void Start()
    {
        dialogueUI = FindObjectOfType<DialogueUI>();
        if (dialogueUI == null)
            Debug.LogError("Không tìm thấy DialogueUI trong scene!");
    }

    void Update()
    {
        if (!playerInRange) return;
        if (dialogueUI == null) return;

        if (Input.GetKeyDown(key))
        {
            Debug.Log("F pressed, show dialogue");
            dialogueUI.Show(dialogue);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("Player vào trigger của NPC");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("Player ra khỏi trigger của NPC");
        }
    }
}
