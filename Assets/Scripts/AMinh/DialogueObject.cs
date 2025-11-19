using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue")]
public class DialogueObject : ScriptableObject
{
    [TextArea] public string[] lines;
}
