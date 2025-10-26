// UIBookDrag.cs — drag a UI element (RectTransform) around a Canvas and drop on UISlot
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIBookDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("Color ID for this book (e.g., 0..5).")]
    public int colorId = 0;

    [Tooltip("Controller that tracks progress and provides slot list.")]
    public UIMiniGamePanelController controller;

    public RectTransform RectT { get; private set; }
    private RectTransform canvasRect;
    private Camera uiCamera;
    private Vector2 startAnchored;
    private bool placed = false;

    private void Awake()
    {
        RectT = GetComponent<RectTransform>();
        startAnchored = RectT.anchoredPosition;
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
            uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }
    }

    private void OnEnable()
    {
        startAnchored = RectT.anchoredPosition;
        placed = false;
    }

    public void ResetToStart()
    {
        placed = false;
        //RectT.anchoredPosition = startAnchored;
    }

    public void SetPlaced(bool value) { placed = value; }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (placed) return;
        startAnchored = RectT.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (placed) return;
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, uiCamera, out localPoint))
        {
            RectT.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (placed) return;

        // Find slot under pointer
        UISlot target = null;
        if (controller != null && controller.slots != null)
        {
            foreach (var s in controller.slots)
            {
                if (s == null) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(s.RectT, eventData.position, uiCamera))
                {
                    target = s;
                    break;
                }
            }
        }

        if (target != null && target.TryPlace(this))
        {
            placed = true;
            controller?.OnBookPlaced();
        }
        else
        {
            // Return to start
            StartCoroutine(SmoothReturn(startAnchored, 0.18f));
        }
    }

    private System.Collections.IEnumerator SmoothReturn(Vector2 target, float duration)
    {
        Vector2 a = RectT.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            RectT.anchoredPosition = Vector2.Lerp(a, target, k);
            yield return null;
        }
        RectT.anchoredPosition = target;
    }
}
