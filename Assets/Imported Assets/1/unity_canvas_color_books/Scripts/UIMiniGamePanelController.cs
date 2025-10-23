using UnityEngine;

public class UIMiniGamePanelController : MonoBehaviour
{
    public GameObject panelRoot;
    public DoorController door;                 // vẫn giữ nếu cần animation mở
    public GameObject sceneTransitionObject;    // ✅ thêm dòng này
    public UIBookDrag[] books;
    public UISlot[] slots;
    public int totalBooks = 6;

    private int placedCount = 0;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (sceneTransitionObject != null) sceneTransitionObject.SetActive(false); // ẩn sẵn
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

            // ✅ Thay vì mở cửa → bật object chuyển scene
            if (sceneTransitionObject != null)
            {
                sceneTransitionObject.SetActive(true);
                if (door != null) door.gameObject.SetActive(false); // ẩn cửa cũ (tùy chọn)
            }
            else if (door != null)
            {
                door.Open(); // fallback
            }
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
