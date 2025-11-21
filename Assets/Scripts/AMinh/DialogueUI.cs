using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text nameText;          // Text hiện tên NPC
    public TMP_Text text;              // Text hiện nội dung thoại

    [Header("Typing Settings")]
    public float typeSpeed = 0.03f;

    DialogueObject currentDialogue;
    int index;
    bool isTyping;
    string currentLine;
    Coroutine typingRoutine;

    // Nếu bạn muốn, có thể để DialoguePanel tắt sẵn trong Hierarchy,
    // script sẽ bật nó khi Show() được gọi.

    // Hàm cũ (nếu đâu đó vẫn gọi Show(dialogue) không có tên)
    public void Show(DialogueObject d)
    {
        Show(d, "");
    }

    // Hàm mới: Show + tên người nói
    public void Show(DialogueObject d, string speakerName)
    {
        currentDialogue = d;
        index = 0;

        if (nameText != null)
            nameText.text = speakerName;

        gameObject.SetActive(true);
        Next();
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (currentDialogue == null) return;

            if (isTyping)
            {
                // Đang gõ dở → hiện full câu
                if (typingRoutine != null) StopCoroutine(typingRoutine);
                text.text = currentLine;
                isTyping = false;
            }
            else
            {
                // Gõ xong → sang câu tiếp theo
                Next();
            }
        }
    }

    void Next()
    {
        if (currentDialogue == null) return;

        if (index >= currentDialogue.lines.Length)
        {
            // Hết thoại
            currentDialogue = null;
            gameObject.SetActive(false);
            return;
        }

        currentLine = currentDialogue.lines[index];
        index++;

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeLine(currentLine));
    }

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        text.text = "";

        foreach (char c in line)
        {
            text.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
    }
}
