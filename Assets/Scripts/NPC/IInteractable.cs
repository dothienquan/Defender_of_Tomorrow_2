using UnityEngine;

public interface IInteractable
{
    string DisplayName { get; }
    void Interact(GameObject interactor);
}

