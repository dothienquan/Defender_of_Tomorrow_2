using UnityEngine;

[DisallowMultipleComponent]
public class Interactable : MonoBehaviour, IInteractable
{
    [SerializeField] private string displayName = "Vật thể";
    public string DisplayName => string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;

    // Ghi đè ở lớp con để tạo hành vi riêng
    public virtual void Interact(GameObject interactor)
    {
        Debug.Log($"[{nameof(Interactable)}] {DisplayName} được tương tác bởi {interactor.name}");
    }
}
