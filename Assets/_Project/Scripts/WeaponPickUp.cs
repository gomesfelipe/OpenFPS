using UnityEngine;

public class WeaponPickUp : MonoBehaviour, IInteractable
{
    public GameObject weaponPrefab;
    [SerializeField] private WeaponHandler _playerWeapon;
    public void OnInteract()
    {
        _playerWeapon = FindObjectOfType<WeaponHandler>();

        if (_playerWeapon != null && weaponPrefab != null)
        {
            GameObject weaponInstance = Instantiate(weaponPrefab);
            var weapon = weaponInstance.GetComponent<WeaponBase>();

            if (weapon != null)
            {
                weapon.SetOwner(_playerWeapon);
                _playerWeapon.EquipWeapon(weapon);
                Debug.Log($"Weapon {weapon.name} given to the player.");
            }
            else
            {
                Debug.LogWarning("Weapon prefab doesn't have WeaponBase.");
                Destroy(weaponInstance);
            }
        }
        gameObject.SetActive(false);
        Destroy(this);
    }
}
