using UnityEngine;

public class FireWeapon : WeaponBase
{

    [Header("Ammunition")]
    public int maxAmmoInClip = 30, totalAmmo = 90;
    private int currentAmmoInClip;
    public bool infiniteAmmo = false;

    [Header("Fire Logic")]
    [SerializeField] protected Transform firePoint;
    public float projectileSpeed = 50f;
    public GameObject projectilePrefab;
    public LayerMask hitMask;

    private void Awake()
    {
        currentAmmoInClip = maxAmmoInClip;
    }

    public override void Fire()
    {
        if (!CanFire || weaponType != WeaponType.Melee && currentAmmoInClip <= 0) return;

        lastShotTime = Time.time;
        currentAmmoInClip--;

        if (projectilePrefab != null)
        {
            // Disparo com projétil
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            if (proj.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = firePoint.forward * projectileSpeed;
            }
            // Passar dano ao projétil se necessário
            if (proj.TryGetComponent<Projectile>(out var projectile))
            {
                projectile.damage = attackDamage;
                projectile.owner = owner;
            }
        }
        else
        {
            // Hitscan
            if (Physics.Raycast(firePoint.position, firePoint.forward, out RaycastHit hit, attackRange, hitMask))
            {
                if (hit.collider.TryGetComponent<IDamageable>(out var target))
                {
                    target.TakeDamage(attackDamage);
                }
            }
        }
    }

    public override void Reload()
    {
        if (infiniteAmmo || totalAmmo <= 0 || currentAmmoInClip == maxAmmoInClip) return;

        int ammoToReload = maxAmmoInClip - currentAmmoInClip;
        int ammoReloaded = Mathf.Min(ammoToReload, totalAmmo);

        totalAmmo -= ammoReloaded;
        currentAmmoInClip += ammoReloaded;
    }
        // Gizmo opcional para depuração
    private void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;

        Gizmos.color = Color.red;
        Vector3 center = firePoint.position + firePoint.forward * attackRange;
        Gizmos.DrawWireSphere(center, attackRadius);
    }

}
