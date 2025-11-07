using UnityEngine;


public abstract class ItemBase : ScriptableObject
{
    [Tooltip("Unique stable ID (set manually or by Create button).")]
    public string id;
    public string itemName;
    public Sprite icon;
}