// DoorController.cs — same door; click to show UI panel; opens upward after puzzle complete
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class DoorController : MonoBehaviour, IPointerClickHandler
{
    public float openDistance = 3f;
    public float openDuration = 0.7f;

    public UnityEvent onDoorOpened;
    public UnityEvent onDoorClicked;

    private bool opened = false;
    private bool opening = false;

    public void Open()
    {
        if (opened || opening) return;
        opening = true;
        StartCoroutine(MoveUp());
    }

    private System.Collections.IEnumerator MoveUp()
    {
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.up * openDistance;
        float t = 0f;
        while (t < openDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / openDuration);
            transform.position = Vector3.Lerp(start, end, k);
            yield return null;
        }
        transform.position = end;
        opening = false;
        opened = true;
        onDoorOpened?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (opened) return; // Only clickable before opened
        onDoorClicked?.Invoke(); // wire this to UIMiniGamePanelController.OpenPanel
    }
}
