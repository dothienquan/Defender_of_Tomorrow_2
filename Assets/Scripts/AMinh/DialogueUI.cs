using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text nameText;          // Text hiện tên NPC
    public TMP_Text text;              // Text hiện nội dung thoại
    public Image avatarImage;          // Image hiển thị avatar của NPC

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
        Show(d, "", null);
    }

    // Hàm với tên người nói (backward compatible)
    public void Show(DialogueObject d, string speakerName)
    {
        Show(d, speakerName, null);
    }

    // Hàm mới: Show + tên người nói + avatar
    public void Show(DialogueObject d, string speakerName, Sprite avatar)
    {
        currentDialogue = d;
        index = 0;

        if (nameText != null)
            nameText.text = speakerName;

        // Hiển thị avatar nếu có
        if (avatarImage != null)
        {
            if (avatar != null)
            {
                avatarImage.sprite = avatar;
                avatarImage.gameObject.SetActive(true);
                
                // Đảm bảo image giữ đúng scaling theo sprite gốc
                avatarImage.preserveAspect = true;
                avatarImage.SetNativeSize();
            }
            else
            {
                // Nếu không có avatar, ẩn image
                avatarImage.gameObject.SetActive(false);
            }
        }

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
