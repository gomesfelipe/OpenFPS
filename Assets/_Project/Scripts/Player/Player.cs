using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class Player : MonoBehaviour
{
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
    private PlayerInputActions _inputActions;

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
        playerInteraction ??= GetComponent<PlayerInteraction>();
        playerInteraction?.Initialize();
        weaponHandler ??= GetComponent<WeaponHandler>();
        weaponHandler?.Initialize();
        playerHealth ??= GetComponent<PlayerHealth>();
        playerHealth?.Initialize();
        playerUI ??= GetComponent<PlayerUI>();
        playerUI?.Initialize();
        if (playerHealth != null && playerUI !=null)
        {
        playerHealth.OnDamageTaken += (amount, current) => playerUI.UpdateHealth(current);
        playerHealth.OnHealthRestored += (amount, current) => playerUI.UpdateHealth(current);
        playerHealth.OnDeath += () => Debug.Log("Died");            
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
        var cameraInput = new CameraInput { Look = input.Look.ReadValue<Vector2>() };
        playerCamera.UpdateRotation(cameraInput);
        //Get character input and update it.
        var characterInput = new CharacterInput
        {
            Rotation = playerCamera.transform.rotation,
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
    public void Teleport(Vector3 position)
    {
        playerCharacter.SetPosition(position);
    }
}
