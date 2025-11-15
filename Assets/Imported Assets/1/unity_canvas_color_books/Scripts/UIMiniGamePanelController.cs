using UnityEngine;

public class UIMiniGamePanelController : MonoBehaviour
{
    public GameObject panelRoot;
    
    
    public UIBookDrag[] books;
    public UISlot[] slots;
    public int totalBooks = 6;

    private int placedCount = 0;
    public MiniGameMover mover;

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
            Debug.Log("Mini-game hoàn thành!");
            ClosePanel();

            if (mover != null)
                mover.OnMiniGameComplete();


        }
    }

    public void ResetPuzzle()
    {
        placedCount = 0;
        if (slots != null)
            foreach (var s in slots) if (s != null) s.ResetOccupied();

        if (books != null)
            foreach (var b in books) if (b != null) b.ResetToStart();
    }
}
