using UnityEngine;


public class Slot : MonoBehaviour
{
    [Tooltip("UI Item GameObject currently in this slot (kept for compatibility)")]
    public GameObject currentItem;


    [Header("Slot Kind")]
    public bool isHotbarSlot = false; // set true for hotbar slots


    // Helper: returns UIItem on currentItem
    public UIItem GetUIItem()
    {
        if (!currentItem) return null;
        return currentItem.GetComponent<UIItem>();
    }
}