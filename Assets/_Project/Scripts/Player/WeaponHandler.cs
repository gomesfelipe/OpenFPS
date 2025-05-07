using UnityEngine;
public class WeaponHandler : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private WeaponBase startingWeapon, _currentWeapon;
    private bool _requestedAttack, _requestedSustainedAttack, _requestedReload;

    public event System.Action<WeaponBase> OnWeaponEquipped, OnWeaponUnequipped;
    private void Start()
    {
        if (startingWeapon != null)
        {
            EquipWeapon(Instantiate(startingWeapon, weaponHolder));
        }
    }
    public void Initialize()
    {
    
    }
    public void EquipWeapon(WeaponBase weapon)
    {
        if (_currentWeapon != null)
            Destroy(_currentWeapon.gameObject);
            _currentWeapon = weapon;
            _currentWeapon.transform.SetParent(weaponHolder, false);
            _currentWeapon.transform.localPosition = Vector3.zero;
            _currentWeapon.transform.localRotation = Quaternion.identity;
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
    
}
