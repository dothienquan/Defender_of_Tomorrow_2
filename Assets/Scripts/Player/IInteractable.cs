using UnityEngine;

public interface IInteractables
{
    string DisplayName { get; }
    void Interact(GameObject interactor);
}
