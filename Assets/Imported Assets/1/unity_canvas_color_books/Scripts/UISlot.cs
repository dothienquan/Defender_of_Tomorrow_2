// UISlot.cs — UI slot with colorId (Canvas)
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UISlot : MonoBehaviour
{
    [Tooltip("Color ID that this slot accepts (e.g., 0..5).")]
    public int colorId;
    [Tooltip("Optional snap offset in anchored units.")]
    public Vector2 snapOffset;
    public bool occupied { get; private set; }

    public RectTransform RectT { get; private set; }

    private void Awake()
    {
        RectT = GetComponent<RectTransform>();
    }

    public void ResetOccupied() { occupied = false; }

    public bool TryPlace(UIBookDrag book)
    {
        if (occupied) return false;
        if (book.colorId != colorId) return false;

        // Snap to slot
        var bookRT = book.RectT;
        bookRT.anchoredPosition = RectT.anchoredPosition + snapOffset;
        book.SetPlaced(true);
        occupied = true;
        return true;
    }
}
