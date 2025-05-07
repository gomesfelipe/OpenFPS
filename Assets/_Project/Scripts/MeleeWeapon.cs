using UnityEngine;

public class MeleeWeapon : WeaponBase
{

    [Header("Melee Settings")]
    private Camera cam;
    public GameObject hitEffect;

    public float meleeForwardOffset = 1f;
    public GameObject meleeHitEffect;
    public string[] attackAnimations;

    private int  attackCount, attackIndex = 0;

    protected override void Start()
    {
        base.Start();
        cam = Camera.main;
        weaponType = WeaponType.Melee;
    }

    public override void Fire()
    {
        if (!readyToAttack || attacking) return;

        readyToAttack = false;
        attacking = true;

        Invoke(nameof(ResetAttack), attackSpeed);
        Invoke(nameof(AttackRaycast), attackDelay);

        if (audioSource && attackSound)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(attackSound);
        }

        if (_anim && attackAnimations.Length > 0)
        {
            string anim = attackAnimations[attackIndex];
            _anim.CrossFadeInFixedTime(anim, 0.2f);
            attackIndex = (attackIndex + 1) % attackAnimations.Length;
        }
    }

    public override void Reload()
    {

    }

    void ResetAttack()
    {
        attacking = false;
        readyToAttack = true;
    }

    void AttackRaycast()
    {
        if(Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, attackDistance, attackLayer))
        { 
            HitTarget(hit.point);

            if (hit.transform.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(attackDamage);
            }
        } 
    }

    void HitTarget(Vector3 pos)
    {
        audioSource.pitch = 1;
        audioSource.PlayOneShot(hitSound);

        GameObject GO = Instantiate(hitEffect, pos, Quaternion.identity);
        Destroy(GO, 20);
    }

}
