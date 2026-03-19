using UnityEngine;
public class WeaponHandler : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private WeaponBase startingWeapon, _currentWeapon;
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    private bool _requestedAttack, _requestedSustainedAttack, _requestedReload;

    public event System.Action<WeaponBase> OnWeaponEquipped, OnWeaponUnequipped;
    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        if (startingWeapon != null)
        {
            EquipWeapon(Instantiate(startingWeapon, weaponHolder));
        }
    }
    public void Initialize()
    {
        ResolveReferences();
    }

    public void EquipWeapon(WeaponBase weapon)
    {
        if (weapon == null)
        {
            Debug.LogWarning("Tried to equip a null weapon.", this);
            return;
        }

        weaponHolder ??= transform;

        var weaponRoot = GetWeaponRootTransform(weapon);

        if (_currentWeapon != null)
        {
            OnWeaponUnequipped?.Invoke(_currentWeapon);
            Destroy(GetWeaponRootTransform(_currentWeapon).gameObject);
        }

        _currentWeapon = weapon;

        if (weaponRoot.parent != weaponHolder)
        {
            weaponRoot.SetParent(weaponHolder, false);
        }

        weaponRoot.localPosition = Vector3.zero;
        weaponRoot.localRotation = Quaternion.identity;
        _currentWeapon.SetOwner(this);
        OnWeaponEquipped?.Invoke(_currentWeapon);
    }
    public void UpdateInput(CharacterInput input)
    {
        _requestedAttack = input.Attack;
        _requestedReload = input.Reload;
        _requestedSustainedAttack = input.AttackSustain;
        if (_requestedAttack)
        {
            TryFire();
        }
        if (_requestedReload)
        {
            Reload();
        }
    }
    public void TryFire()
    {
        if (_currentWeapon != null && _currentWeapon.CanFire)
        {
            Debug.Log("Attack");
            _currentWeapon.Fire();
        }
    }

    public void Reload()
    {
        _currentWeapon?.Reload();
    }

    private Transform GetWeaponRootTransform(WeaponBase weapon)
    {
        var current = weapon.transform;

        while (current.parent != null && current.parent != weaponHolder)
        {
            current = current.parent;
        }

        return current;
    }

    public Animator GetCharacterAnimator()
    {
        ResolveReferences();
        return playerCharacter != null ? playerCharacter.GetAnimator() : null;
    }

    public Camera GetOwnerCamera()
    {
        ResolveReferences();

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

        return GetComponentInChildren<Camera>();
    }

    private void ResolveReferences()
    {
        weaponHolder ??= transform;
        playerCharacter ??= GetComponentInChildren<PlayerCharacter>();
        playerCamera ??= GetComponentInChildren<PlayerCamera>();
    }
    
}
