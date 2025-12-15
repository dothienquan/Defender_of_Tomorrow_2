using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [Tooltip("Tên người nói (để trống nếu dùng tên mặc định từ NPCDialogue)")]
    public string speakerName = "";
    
    [TextArea(2, 5)]
    [Tooltip("Nội dung câu thoại")]
    public string text = "";

    [Tooltip("Avatar của người nói (để trống nếu dùng avatar mặc định từ NPCDialogue)")]
    public Sprite avatar;

    public DialogueLine() { }

    public DialogueLine(string text)
    {
        this.text = text;
        this.speakerName = "";
        this.avatar = null;
    }

    public DialogueLine(string speakerName, string text)
    {
        this.speakerName = speakerName;
        this.text = text;
        this.avatar = null;
    }

    public DialogueLine(string speakerName, string text, Sprite avatar)
    {
        this.speakerName = speakerName;
        this.text = text;
        this.avatar = avatar;
    }
}

