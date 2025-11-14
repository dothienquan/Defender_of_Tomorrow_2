using UnityEngine;
using UnityEngine.UI;

public class ImageInteractable : Interactable
{
    [Header("UI Image to Toggle")]
    [SerializeField] private GameObject imagePanel;

    private bool isShowing = false;

    private void Awake()
    {
        if (imagePanel != null)
        {
            imagePanel.SetActive(false); // Ẩn ban đầu
        }
    }

    public override void Interact(GameObject interactor)
    {
        if (imagePanel == null) return;

        isShowing = !isShowing;
        imagePanel.SetActive(isShowing);
    }
}
