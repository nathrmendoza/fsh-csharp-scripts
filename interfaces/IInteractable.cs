using UnityEngine;
public interface IInteractable
{
    void OnInteract();      // Called when player clicks
    void OnFocus();         // Called when player looks at object
    void OnLoseFocus();     // Called when player stops looking at object
    bool CanInteract { get; }    // If interaction is currently possible
    InteractionType InteractionType { get; }
}
