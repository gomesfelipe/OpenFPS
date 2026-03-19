using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class Player : MonoBehaviour
{
    private const string KeyboardMouseBindingGroup = "Keyboard&Mouse";
    private const string GamepadBindingGroup = "Gamepad";
    private const string JoystickBindingGroup = "Joystick";
    private const string XrBindingGroup = "XR";

    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerUI playerUI;
    [Space]
    [SerializeField] private CameraSpring cameraSpring;
    [SerializeField] private CameraLean cameraLean;
    [Space]
    [SerializeField] private WeaponHandler weaponHandler;
    [Space]
    [SerializeField] private Volume volume;
    [SerializeField] private StanceVignette stanceVignette;

    [SerializeField] private GameObject zombiePrefab;

    private PlayerInputActions _inputActions;
    private string _activeBindingGroup = KeyboardMouseBindingGroup;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _inputActions = new PlayerInputActions();
        _inputActions.Enable();
        playerCharacter ??= GetComponentInChildren<PlayerCharacter>();
        playerCharacter?.Initialize();
        playerCamera ??= GetComponentInChildren<PlayerCamera>();
        playerCamera.Initialize(playerCharacter.GetCameraTarget());
        cameraSpring ??= GetComponentInChildren<CameraSpring>();
        cameraSpring?.Initialize();
        cameraLean ??= GetComponentInChildren<CameraLean>();
        cameraLean?.Initialize();
        stanceVignette?.Initialize(volume.profile);
        playerUI ??= GetComponent<PlayerUI>();
        playerUI?.Initialize();
        UpdateInteractionPromptBinding(forceRefresh: true);
        playerInteraction ??= GetComponent<PlayerInteraction>();
        playerInteraction?.Initialize(playerUI);
        weaponHandler ??= GetComponent<WeaponHandler>();
        weaponHandler?.Initialize();
        playerHealth ??= GetComponent<PlayerHealth>();
        playerHealth?.Initialize();
        if (playerHealth != null && playerUI !=null)
        {
        playerHealth.OnDamageTaken += (amount, current) => playerUI.UpdateHealth(current);
        playerHealth.OnHealthRestored += (amount, current) => playerUI.UpdateHealth(current);
        playerHealth.OnDeath += () => Debug.Log("Died");
        playerHealth.OnBecameZombie += TransformIntoZombie;
        }

    }
    private void OnDestroy()
    {
        _inputActions.Dispose();
    }
    void Update()
    {
        var input = _inputActions.Player;
        var deltaTime = Time.deltaTime;
        //Get camera input and update it's rotation.
        var lookControl = input.Look.activeControl;
        var cameraInput = new CameraInput
        {
            Look = input.Look.ReadValue<Vector2>(),
            UseDeltaTime = lookControl != null && lookControl.device is not Mouse
        };
        playerCamera.UpdateRotation(cameraInput);
        //Get character input and update it.
        var characterInput = new CharacterInput
        {
            Rotation = playerCamera.GetRotation(),
            Move = input.Move.ReadValue<Vector2>(),
            Jump = input.Jump.WasPerformedThisFrame(),
            JumpSustain = input.Jump.IsPressed(),
            Crouch = input.Crouch.WasPerformedThisFrame() ? CrouchInput.Toggle : CrouchInput.None,
            Interact = input.Interact.WasPerformedThisFrame(),
            Attack = input.Attack.WasPerformedThisFrame(),
            AttackSustain = input.Attack.IsPressed(),
            Reload = input.Reload.WasPerformedThisFrame()
        };
        playerCharacter.UpdateInput(characterInput);
        playerCharacter.UpdateBody(deltaTime);
        playerInteraction.UpdateInput(characterInput);
        weaponHandler.UpdateInput(characterInput);
        UpdateInteractionPromptBinding();
    }

    private void LateUpdate()
    {
        var deltaTime = Time.deltaTime;
        var cameraTarget = playerCharacter.GetCameraTarget();
        var state = playerCharacter.GetState();
        playerCamera.UpdatePosition(cameraTarget);
        if (cameraSpring != null && cameraSpring.isActiveAndEnabled)
        {
            cameraSpring.UpdateSpring(deltaTime, cameraTarget.up);
        }
        cameraLean.UpdateLean
            (
            deltaTime,
            state.Stance is Stance.Slide,
            state.Acceleration,
            cameraTarget.up
            );
        stanceVignette.UpdateVignette(deltaTime, state.Stance);
    }
    private void TransformIntoZombie()
    {
        // Save position
        transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

        // Instancia zumbi
        var zombieInstance = Instantiate(zombiePrefab, position, rotation);

        if (zombieInstance.TryGetComponent<Enemy>(out var zombieEnemy))
        {
            // Opcional: defina o alvo para zumbis humanos restantes
            GameObject playerTarget = FindObjectOfType<Player>()?.gameObject;
            if (playerTarget != null)
            {
                zombieEnemy.SetTarget(playerTarget.transform);
            }
        }

        // Destroi o jogador original
        Destroy(gameObject);
    }
    public void Teleport(Vector3 position)
    {
        playerCharacter.SetPosition(position);
    }

    private void UpdateInteractionPromptBinding(bool forceRefresh = false)
    {
        if (_inputActions == null || playerUI == null)
        {
            return;
        }

        var resolvedBindingGroup = ResolveActiveBindingGroup();
        if (!forceRefresh && resolvedBindingGroup == _activeBindingGroup)
        {
            return;
        }

        _activeBindingGroup = resolvedBindingGroup;

        var bindingLabel = _inputActions.Player.Interact.GetBindingDisplayString(
            InputBinding.DisplayStringOptions.DontIncludeInteractions,
            _activeBindingGroup);
        var bindingPath = ResolveBindingPath(_inputActions.Player.Interact, _activeBindingGroup);

        if (string.IsNullOrWhiteSpace(bindingLabel) && _activeBindingGroup != KeyboardMouseBindingGroup)
        {
            bindingLabel = _inputActions.Player.Interact.GetBindingDisplayString(
                InputBinding.DisplayStringOptions.DontIncludeInteractions,
                KeyboardMouseBindingGroup);
            bindingPath = ResolveBindingPath(_inputActions.Player.Interact, KeyboardMouseBindingGroup);
        }

        playerUI.SetInteractionBindingLabel(bindingLabel, _activeBindingGroup, bindingPath);
    }

    private string ResolveActiveBindingGroup()
    {
        var playerActions = _inputActions.Player;

        if (TryResolveBindingGroup(playerActions.Interact.activeControl, out var bindingGroup) ||
            TryResolveBindingGroup(playerActions.Attack.activeControl, out bindingGroup) ||
            TryResolveBindingGroup(playerActions.Reload.activeControl, out bindingGroup) ||
            TryResolveBindingGroup(playerActions.Crouch.activeControl, out bindingGroup) ||
            TryResolveBindingGroup(playerActions.Jump.activeControl, out bindingGroup) ||
            TryResolveBindingGroup(playerActions.Move.activeControl, out bindingGroup) ||
            TryResolveBindingGroup(playerActions.Look.activeControl, out bindingGroup))
        {
            return bindingGroup;
        }

        return _activeBindingGroup;
    }

    private static bool TryResolveBindingGroup(InputControl control, out string bindingGroup)
    {
        if (control == null)
        {
            bindingGroup = null;
            return false;
        }

        var device = control.device;
        if (device is Gamepad)
        {
            bindingGroup = GamepadBindingGroup;
            return true;
        }

        if (device is Joystick)
        {
            bindingGroup = JoystickBindingGroup;
            return true;
        }

        if (device is Keyboard || device is Mouse)
        {
            bindingGroup = KeyboardMouseBindingGroup;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(device.layout) &&
            device.layout.IndexOf("XR", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            bindingGroup = XrBindingGroup;
            return true;
        }

        bindingGroup = null;
        return false;
    }

    private static string ResolveBindingPath(InputAction action, string bindingGroup)
    {
        var bindings = action.bindings;
        for (int index = 0; index < bindings.Count; index++)
        {
            var binding = bindings[index];
            if (binding.isComposite || binding.isPartOfComposite)
            {
                continue;
            }

            if (!BindingBelongsToGroup(binding.groups, bindingGroup))
            {
                continue;
            }

            return string.IsNullOrWhiteSpace(binding.effectivePath)
                ? binding.path
                : binding.effectivePath;
        }

        return null;
    }

    private static bool BindingBelongsToGroup(string bindingGroups, string targetGroup)
    {
        if (string.IsNullOrWhiteSpace(bindingGroups) || string.IsNullOrWhiteSpace(targetGroup))
        {
            return false;
        }

        var groups = bindingGroups.Split(';');
        for (int index = 0; index < groups.Length; index++)
        {
            if (string.Equals(groups[index], targetGroup, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
