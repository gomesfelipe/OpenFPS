using UnityEngine;
public class WeaponHandler : MonoBehaviour
{
    private const int MaxQuickSlots = 4;

    [SerializeField] private Transform weaponHolder;
    [SerializeField] private WeaponBase startingWeapon, _currentWeapon;
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private WeaponBase[] _weaponSlots = new WeaponBase[MaxQuickSlots];
    [SerializeField] private int _activeSlotIndex = -1;
    private bool _requestedAttack, _requestedSustainedAttack, _requestedReload;

    public event System.Action<WeaponBase> OnWeaponEquipped, OnWeaponUnequipped;
    public event System.Action OnQuickSlotsChanged;

    public int ActiveSlotIndex => _activeSlotIndex;
    public int QuickSlotCount => _weaponSlots != null ? _weaponSlots.Length : 0;
    public bool CanAim => _currentWeapon != null && _currentWeapon.Type != WeaponType.Melee;
    public WeaponType? CurrentWeaponType => _currentWeapon != null ? _currentWeapon.Type : null;

    private void Awake()
    {
        ResolveReferences();
        EnsureQuickSlotStorage();
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
        EnsureQuickSlotStorage();
    }

    public void EquipWeapon(WeaponBase weapon)
    {
        EnsureQuickSlotStorage();

        if (weapon == null)
        {
            Debug.LogWarning("Tried to equip a null weapon.", this);
            return;
        }

        int slotIndex = ResolveSlotIndexForWeapon(weapon);
        AssignWeaponToSlot(weapon, slotIndex, true);
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

    public void HandleAttackHitAnimationEvent()
    {
        _currentWeapon?.OnAttackHitAnimationEvent();
    }

    public void HandleAttackRecoveryAnimationEvent()
    {
        _currentWeapon?.OnAttackRecoveryAnimationEvent();
    }

    public bool SelectSlot(int slotIndex)
    {
        EnsureQuickSlotStorage();

        if (!IsValidSlotIndex(slotIndex))
        {
            return false;
        }

        var weapon = _weaponSlots[slotIndex];
        if (weapon == null)
        {
            return false;
        }

        if (_currentWeapon == weapon && _activeSlotIndex == slotIndex)
        {
            AttachWeaponToHolder(weapon);
            SetWeaponActive(weapon, true);
            return true;
        }

        if (_currentWeapon != null)
        {
            SetWeaponActive(_currentWeapon, false);
            OnWeaponUnequipped?.Invoke(_currentWeapon);
        }

        _currentWeapon = weapon;
        _activeSlotIndex = slotIndex;
        AttachWeaponToHolder(_currentWeapon);
        SetWeaponActive(_currentWeapon, true);
        _currentWeapon.SetOwner(this);
        OnWeaponEquipped?.Invoke(_currentWeapon);
        NotifyQuickSlotsChanged();
        return true;
    }

    public WeaponBase GetWeaponInSlot(int slotIndex)
    {
        EnsureQuickSlotStorage();
        return IsValidSlotIndex(slotIndex) ? _weaponSlots[slotIndex] : null;
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

    private void EnsureQuickSlotStorage()
    {
        if (_weaponSlots == null || _weaponSlots.Length != MaxQuickSlots)
        {
            var previousSlots = _weaponSlots;
            _weaponSlots = new WeaponBase[MaxQuickSlots];

            if (previousSlots != null)
            {
                int copyLength = Mathf.Min(previousSlots.Length, _weaponSlots.Length);
                for (int index = 0; index < copyLength; index++)
                {
                    _weaponSlots[index] = previousSlots[index];
                }
            }
        }

        if (_activeSlotIndex >= _weaponSlots.Length)
        {
            _activeSlotIndex = -1;
        }
    }

    private void AssignWeaponToSlot(WeaponBase weapon, int slotIndex, bool selectAfterAssign)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogWarning("Tried to assign a weapon to an invalid quick slot.", this);
            return;
        }

        var previousWeapon = _weaponSlots[slotIndex];
        if (previousWeapon == weapon)
        {
            if (selectAfterAssign)
            {
                SelectSlot(slotIndex);
            }

            return;
        }

        if (previousWeapon != null)
        {
            if (_currentWeapon == previousWeapon)
            {
                SetWeaponActive(previousWeapon, false);
                OnWeaponUnequipped?.Invoke(previousWeapon);
                _currentWeapon = null;
                _activeSlotIndex = -1;
            }

            Destroy(GetWeaponRootTransform(previousWeapon).gameObject);
        }

        _weaponSlots[slotIndex] = weapon;
        AttachWeaponToHolder(weapon);
        SetWeaponActive(weapon, false);
        weapon.SetOwner(this);
        NotifyQuickSlotsChanged();

        if (selectAfterAssign)
        {
            SelectSlot(slotIndex);
        }
    }

    private void AttachWeaponToHolder(WeaponBase weapon)
    {
        if (weapon == null)
        {
            return;
        }

        weaponHolder ??= transform;

        var weaponRoot = GetWeaponRootTransform(weapon);
        var desiredScale = weaponRoot.localScale;
        if (weaponRoot.parent != weaponHolder)
        {
            weaponRoot.SetParent(weaponHolder, false);
        }

        weaponRoot.localPosition = weapon.EquippedLocalPosition;
        weaponRoot.localRotation = Quaternion.Euler(weapon.EquippedLocalEulerAngles);
        weaponRoot.localScale = desiredScale;
    }

    private void SetWeaponActive(WeaponBase weapon, bool isActive)
    {
        if (weapon == null)
        {
            return;
        }

        var weaponRoot = GetWeaponRootTransform(weapon);
        if (weaponRoot.gameObject.activeSelf != isActive)
        {
            weaponRoot.gameObject.SetActive(isActive);
        }
    }

    private int ResolveSlotIndexForWeapon(WeaponBase weapon)
    {
        int existingSlotIndex = FindExistingSlotIndex(weapon);
        if (existingSlotIndex >= 0)
        {
            return existingSlotIndex;
        }

        int freeSlotIndex = FindFirstEmptySlotIndex();
        if (freeSlotIndex >= 0)
        {
            return freeSlotIndex;
        }

        return _activeSlotIndex >= 0 ? _activeSlotIndex : 0;
    }

    private int FindExistingSlotIndex(WeaponBase weapon)
    {
        if (weapon == null)
        {
            return -1;
        }

        for (int index = 0; index < _weaponSlots.Length; index++)
        {
            var slotWeapon = _weaponSlots[index];
            if (slotWeapon == null)
            {
                continue;
            }

            if (slotWeapon == weapon || IsSameWeaponKind(slotWeapon, weapon))
            {
                return index;
            }
        }

        return -1;
    }

    private int FindFirstEmptySlotIndex()
    {
        for (int index = 0; index < _weaponSlots.Length; index++)
        {
            if (_weaponSlots[index] == null)
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsSameWeaponKind(WeaponBase left, WeaponBase right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return left.Type == right.Type &&
            string.Equals(left.DisplayName, right.DisplayName, System.StringComparison.OrdinalIgnoreCase);
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < _weaponSlots.Length;
    }

    private void NotifyQuickSlotsChanged()
    {
        OnQuickSlotsChanged?.Invoke();
    }
    
}
