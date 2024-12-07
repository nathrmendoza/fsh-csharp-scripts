using UnityEngine;
public class TestInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private InteractionType interactionType = InteractionType.Interact;
    public InteractionType InteractionType => interactionType;

    public bool CanInteract { get; private set; } = true;

    public void OnInteract()
    {
        Debug.Log("Interacted with cube");
    }

    public void OnFocus()
    {
        Debug.Log("Focusing on cube");
    }

    public void OnLoseFocus()
    {
        Debug.Log("Lost focus on cube");
    }
}