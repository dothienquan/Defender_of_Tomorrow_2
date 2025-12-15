using UnityEngine;

/// <summary>
/// ScriptableObject để lưu thông tin các nhân vật trong dialogue
/// Có thể tái sử dụng cho nhiều dialogue khác nhau
/// </summary>
[CreateAssetMenu(menuName = "Dialogue/Speaker Data")]
public class DialogueSpeakerData : ScriptableObject
{
    [Header("Speaker Info")]
    [Tooltip("Tên nhân vật")]
    public string speakerName = "";
    
    [Tooltip("Avatar của nhân vật")]
    public Sprite avatar;
}

