using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactableLayerMask = ~0;
    [SerializeField] private bool _requestedInteraction, _isInteracting;
    [SerializeField] IInteractable currentInteractable;
    public void Initialize()
    {
    }
    public void UpdateInput(CharacterInput input)
    {
        _requestedInteraction = input.Interact;
        if (_requestedInteraction)
        {
            TryInteract();
        }
    }
    public void TryInteract()
    {
        _isInteracting = true;

        Vector3 origin = Camera.main.transform.position;
        Vector3 direction = Camera.main.transform.forward;
        Ray r = new(origin, direction);
        //If colliders with anything within player reach
        if (Physics.Raycast(r, out RaycastHit hit, interactDistance))
        {
             Debug.Log("Raycast acertou: " + hit.collider.name);
            if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
            {
                interactable.OnInteract();
            }
            else if (hit.collider.GetComponentInParent<IInteractable>() is { } parentInteractable)
            {
                parentInteractable.OnInteract();
            }
            else // if new interactable is not found
            {
                DisableCurrentInteractable();
            }
        }
        else //if nothing reach
        {
            DisableCurrentInteractable();
        }

    }

    void SetNewCurrentInteractable(IInteractable newInteractable)
    {
        currentInteractable = newInteractable;
    }

    void DisableCurrentInteractable()
    {
        if (currentInteractable != null)
        {
            currentInteractable = null;
        }
        _isInteracting = false;
    }        
}