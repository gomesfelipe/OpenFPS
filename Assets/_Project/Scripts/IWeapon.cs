public interface IWeapon
{
    void Fire();
    void Reload();
    bool CanFire { get; }
    void SetOwner(WeaponHandler owner);
}
