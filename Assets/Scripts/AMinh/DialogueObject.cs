using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue")]
public class DialogueObject : ScriptableObject
{
    [Header("Legacy Format (Backward Compatible)")]
    [Tooltip("Format cũ: chỉ có text, dùng tên và avatar từ NPCDialogue. Để trống nếu dùng Multi-Speaker Format.")]
    [TextArea] public string[] lines;

    [Header("Multi-Speaker Format")]
    [Tooltip("Format mới: mỗi dòng có thể có người nói riêng. Ưu tiên format này nếu có dữ liệu.")]
    public DialogueLine[] dialogueLines;

    /// <summary>
    /// Lấy số lượng dòng dialogue (hỗ trợ cả 2 format)
    /// </summary>
    public int LineCount
    {
        get
        {
            if (dialogueLines != null && dialogueLines.Length > 0)
                return dialogueLines.Length;
            if (lines != null)
                return lines.Length;
            return 0;
        }
    }

    /// <summary>
    /// Kiểm tra xem có dùng format mới không
    /// </summary>
    public bool UsesMultiSpeakerFormat => dialogueLines != null && dialogueLines.Length > 0;

    /// <summary>
    /// Lấy DialogueLine tại index (hỗ trợ cả 2 format)
    /// </summary>
    public DialogueLine GetLine(int index, string defaultSpeakerName = "", Sprite defaultAvatar = null)
    {
        if (UsesMultiSpeakerFormat)
        {
            if (index >= 0 && index < dialogueLines.Length)
            {
                DialogueLine line = dialogueLines[index];
                // Nếu speakerName trống, dùng defaultSpeakerName
                if (string.IsNullOrEmpty(line.speakerName))
                    line.speakerName = defaultSpeakerName;
                // Nếu avatar null, dùng defaultAvatar
                if (line.avatar == null)
                    line.avatar = defaultAvatar;
                return line;
            }
        }
        else
        {
            // Format cũ: chuyển đổi sang DialogueLine
            if (index >= 0 && index < lines.Length)
            {
                return new DialogueLine(defaultSpeakerName, lines[index], defaultAvatar);
            }
        }

        return new DialogueLine(defaultSpeakerName, "", defaultAvatar);
    }
}
