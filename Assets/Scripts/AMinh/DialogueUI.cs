using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    public TMP_Text text;
    public float typeSpeed = 0.03f;

    DialogueObject currentDialogue;
    int index;
    bool isTyping;
    string currentLine;
    Coroutine typingRoutine;

    void Start()
    {
        // Lúc bắt đầu game tự ẩn, nhưng object phải ACTIVE để Start chạy
        gameObject.SetActive(false);
    }

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

        if (Input.GetMouseButtonDown(0))
        {
            if (currentDialogue == null) return;

            if (isTyping)
            {
                StopCoroutine(typingRoutine);
                text.text = currentLine;
                isTyping = false;
            }
            else
            {
                Next();
            }
        }
    }

    void Next()
    {
        if (currentDialogue == null) return;

        if (index >= currentDialogue.lines.Length)
        {
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
