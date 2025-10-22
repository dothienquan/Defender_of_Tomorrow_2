// UIMiniGamePanelController.cs — manages the Canvas panel mini-game
using UnityEngine;

public class UIMiniGamePanelController : MonoBehaviour
{
    [Header("Panel Root (Canvas child)")]
    public GameObject panelRoot;     // disable by default

    [Header("Glue to open door when finished")]
    public DoorController door;

    [Header("Wiring")]
    public UIBookDrag[] books;
    public UISlot[] slots;
    public int totalBooks = 6;

    private int placedCount = 0;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void OpenPanel()
    {
        ResetPuzzle();
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void OnBookPlaced()
    {
        placedCount++;
        if (placedCount >= totalBooks)
        {
            ClosePanel();
            if (door != null) door.Open();
        }
    }

    public void ResetPuzzle()
    {
        placedCount = 0;
        if (slots != null)
        {
            foreach (var s in slots) if (s != null) s.ResetOccupied();
        }
        if (books != null)
        {
            foreach (var b in books) if (b != null) b.ResetToStart();
        }
    }
}
