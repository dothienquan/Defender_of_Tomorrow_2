using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    public TMP_Text text;          // Kéo TextMeshPro vào đây trong Inspector
    public float typeSpeed = 0.03f; // Thời gian giữa mỗi ký tự

    DialogueObject currentDialogue;
    int index;
    bool isTyping;
    string currentLine;
    Coroutine typingRoutine;

    public void Show(DialogueObject d)
    {
        currentDialogue = d;
        index = 0;
        gameObject.SetActive(true);
        Next();
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;

        if (Input.GetMouseButtonDown(0)) // click chuột trái
        {
            OnClick();
        }
    }

    void OnClick()
    {
        if (currentDialogue == null) return;

        if (isTyping)
        {
            // Nếu đang gõ → hiện full câu luôn
            if (typingRoutine != null) StopCoroutine(typingRoutine);
            text.text = currentLine;
            isTyping = false;
        }
        else
        {
            // Đã gõ xong câu → sang câu tiếp theo
            Next();
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
