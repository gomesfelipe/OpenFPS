using UnityEngine;

public class FireWeapon : WeaponBase
{
    public event System.Action<int, int> OnAmmoChanged;

    [Header("Animation")]
    [SerializeField] private string fireTriggerName = "Fire";
    [SerializeField] private string reloadTriggerName = "Reload";

    [Header("Ammunition")]
    public int maxAmmoInClip = 30, totalAmmo = 90;
    private int currentAmmoInClip;
    public bool infiniteAmmo = false;

    public int CurrentAmmoInClip => currentAmmoInClip;
    public int TotalAmmo => totalAmmo;

    [Header("Fire Logic")]
    [SerializeField] protected Transform firePoint;
    public float projectileSpeed = 50f;
    public GameObject projectilePrefab;
    public LayerMask hitMask;

    private void Awake()
    {
        currentAmmoInClip = maxAmmoInClip;
    }

    public override void SetOwner(WeaponHandler character)
    {
        base.SetOwner(character);

        if (firePoint == null)
        {
            firePoint = ResolveFallbackFirePoint();
        }

        NotifyAmmoChanged();
    }

    public override void Fire()
    {
        if (!CanFire || weaponType != WeaponType.Melee && currentAmmoInClip <= 0) return;

        if (firePoint == null)
        {
            firePoint = ResolveFallbackFirePoint();
        }

        if (firePoint == null)
        {
            Debug.LogWarning("Fire weapon failed because no fire point or owner camera was resolved.", this);
            return;
        }

        lastShotTime = Time.time;
        currentAmmoInClip--;
        TriggerOwnerAnimatorIfAvailable(fireTriggerName);
        NotifyAmmoChanged();

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
            var aimOrigin = ownerCamera != null ? ownerCamera.transform.position : firePoint.position;
            var aimDirection = ownerCamera != null ? ownerCamera.transform.forward : firePoint.forward;

            if (Physics.Raycast(aimOrigin, aimDirection, out RaycastHit hit, attackRange, hitMask))
            {
                var target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null)
                {
                    target.TakeDamage(attackDamage);
                }
            }
        }
    }

    public override void Reload()
    {
        if (infiniteAmmo || totalAmmo <= 0 || currentAmmoInClip == maxAmmoInClip) return;

        TriggerOwnerAnimatorIfAvailable(reloadTriggerName);

        int ammoToReload = maxAmmoInClip - currentAmmoInClip;
        int ammoReloaded = Mathf.Min(ammoToReload, totalAmmo);

        totalAmmo -= ammoReloaded;
        currentAmmoInClip += ammoReloaded;
        NotifyAmmoChanged();
    }
        // Gizmo opcional para depuração
    private void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;

        Gizmos.color = Color.red;
        Vector3 center = firePoint.position + firePoint.forward * attackRange;
        Gizmos.DrawWireSphere(center, attackRadius);
    }

    private Transform ResolveFallbackFirePoint()
    {
        return ownerCamera != null ? ownerCamera.transform : transform;
    }

    private void NotifyAmmoChanged()
    {
        OnAmmoChanged?.Invoke(currentAmmoInClip, infiniteAmmo ? maxAmmoInClip : totalAmmo);
    }

}
