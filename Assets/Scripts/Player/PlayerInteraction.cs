using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private InteractionUI interactionUI;

    [Header("Optional")]
    [SerializeField] private LayerMask interactableLayers = ~0; // mặc định: mọi layer

    private readonly List<IInteractable> candidates = new List<IInteractable>();
    private IInteractable current;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // vùng tương tác
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsLayerAllowed(other.gameObject.layer)) return;

        var interactable = other.GetComponent<IInteractable>();
        if (interactable == null)
        {
            // cũng hỗ trợ component Interactable (implements IInteractable)
            interactable = other.GetComponent<Interactable>();
        }

        if (interactable != null && !candidates.Contains(interactable))
        {
            candidates.Add(interactable);
            UpdateCurrent();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var interactable = other.GetComponent<IInteractable>() ?? (IInteractable)other.GetComponent<Interactable>();
        if (interactable != null)
        {
            candidates.Remove(interactable);
            if (current == interactable) current = null;
            UpdateCurrent();
        }
    }

    private void Update()
    {
        if (current != null && Input.GetKeyDown(KeyCode.F))
        {
            current.Interact(gameObject);
        }
    }

    private void UpdateCurrent()
    {
        // Ở ví dụ này, chọn ứng viên gần nhất
        IInteractable nearest = null;
        float best = float.PositiveInfinity;
        foreach (var c in candidates)
        {
            var mb = c as MonoBehaviour;
            if (mb == null) continue;
            float d = Vector2.SqrMagnitude(mb.transform.position - transform.position);
            if (d < best)
            {
                best = d;
                nearest = c;
            }
        }
        current = nearest;

        if (current != null)
        {
            interactionUI?.Show(current.DisplayName);
        }
        else
        {
            interactionUI?.Hide();
        }
    }

    private bool IsLayerAllowed(int layer)
    {
        return (interactableLayers & (1 << layer)) != 0;
    }
}
