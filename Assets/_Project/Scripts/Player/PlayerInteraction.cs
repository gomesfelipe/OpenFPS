using UnityEngine;
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private PlayerUI playerUI;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private float interactRadius = 0.3f;
    [SerializeField] private LayerMask interactableLayerMask = ~0;
    [SerializeField] private bool _requestedInteraction, _isInteracting;
    [SerializeField] IInteractable currentInteractable;
    private InteractableOutlineTarget _currentOutlineTarget;

    public void Initialize(PlayerUI ui = null)
    {
        playerCamera ??= GetComponentInChildren<PlayerCamera>();
        playerUI = ui != null ? ui : GetComponent<PlayerUI>();
        playerUI?.HideInteractionPrompt();
    }

    public void UpdateInput(CharacterInput input)
    {
        UpdateCurrentInteractable();
        _requestedInteraction = input.Interact;
        if (_requestedInteraction && currentInteractable != null)
        {
            _isInteracting = true;
            currentInteractable.OnInteract();
            return;
        }

        if (_requestedInteraction)
        {
            TryInteract();
        }
    }

    public void TryInteract()
    {
        _isInteracting = true;

        var interactionCamera = ResolveInteractionCamera();
        if (interactionCamera == null)
        {
            Debug.LogWarning("No camera found for player interaction.", this);
            DisableCurrentInteractable();
            return;
        }

        if (TryFindInteractable(interactionCamera, out var interactable, out var outlineTarget))
        {
            SetNewCurrentInteractable(interactable, outlineTarget);
            interactable.OnInteract();
            return;
        }

        DisableCurrentInteractable();

    }

    private void UpdateCurrentInteractable()
    {
        var interactionCamera = ResolveInteractionCamera();
        if (interactionCamera == null)
        {
            DisableCurrentInteractable();
            return;
        }

        if (TryFindInteractable(interactionCamera, out var interactable, out var outlineTarget))
        {
            SetNewCurrentInteractable(interactable, outlineTarget);
            return;
        }

        DisableCurrentInteractable();
    }

    private bool TryFindInteractable(Camera interactionCamera, out IInteractable interactable, out InteractableOutlineTarget outlineTarget)
    {
        Vector3 origin = interactionCamera.transform.position;
        Vector3 direction = interactionCamera.transform.forward;
        Ray r = new(origin, direction);

        var hits = Physics.SphereCastAll(r, interactRadius, interactDistance, interactableLayerMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int index = 0; index < hits.Length; index++)
        {
            var hit = hits[index];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (!TryGetInteractable(hit.collider, out interactable))
            {
                continue;
            }

            outlineTarget = hit.collider.GetComponentInParent<InteractableOutlineTarget>();
            return true;
        }

        interactable = null;
        outlineTarget = null;
        return false;
    }

    private Camera ResolveInteractionCamera()
    {
        if (playerCamera != null)
        {
            var cameraComponent = playerCamera.GetComponent<Camera>();
            if (cameraComponent != null)
            {
                return cameraComponent;
            }

            cameraComponent = playerCamera.GetComponentInChildren<Camera>();
            if (cameraComponent != null)
            {
                return cameraComponent;
            }
        }

        return Camera.main;
    }

    private bool TryGetInteractable(Collider hitCollider, out IInteractable interactable)
    {
        if (hitCollider.TryGetComponent<IInteractable>(out interactable))
        {
            return true;
        }

        interactable = hitCollider.GetComponentInParent<IInteractable>();
        return interactable != null;
    }

    void SetNewCurrentInteractable(IInteractable newInteractable)
    {
        SetNewCurrentInteractable(newInteractable, _currentOutlineTarget);
    }

    void SetNewCurrentInteractable(IInteractable newInteractable, InteractableOutlineTarget outlineTarget)
    {
        if (ReferenceEquals(currentInteractable, newInteractable) && _currentOutlineTarget == outlineTarget)
        {
            return;
        }

        if (_currentOutlineTarget != null)
        {
            _currentOutlineTarget.SetHighlighted(false);
        }

        currentInteractable = newInteractable;
        _currentOutlineTarget = outlineTarget;

        if (_currentOutlineTarget != null)
        {
            _currentOutlineTarget.SetHighlighted(true);
        }

        playerUI?.ShowInteractionPrompt(GetPromptVerb(newInteractable));
    }

    void DisableCurrentInteractable()
    {
        if (_currentOutlineTarget != null)
        {
            _currentOutlineTarget.SetHighlighted(false);
            _currentOutlineTarget = null;
        }

        if (currentInteractable != null)
        {
            currentInteractable = null;
        }

        playerUI?.HideInteractionPrompt();
        _isInteracting = false;
    }

    private static string GetPromptVerb(IInteractable interactable)
    {
        return interactable is IInteractionPromptProvider promptProvider
            ? promptProvider.GetInteractionPromptVerb()
            : "interact";
    }
}