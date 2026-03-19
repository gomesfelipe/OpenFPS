using UnityEngine;

public class MeleeWeapon : WeaponBase
{

    [Header("Melee Settings")]
    private Camera cam;
    public GameObject hitEffect;
    [SerializeField] private bool useOwnerCharacterAnimator = true;
    [SerializeField] private string attackTriggerName = "Attack";

    public float meleeForwardOffset = 1f;
    public string[] attackAnimations;

    private int  attackCount, attackIndex = 0;

    protected override void Start()
    {
        base.Start();
        ResolveAttackCamera();
        weaponType = WeaponType.Melee;
    }

    public override void SetOwner(WeaponHandler character)
    {
        base.SetOwner(character);
        ResolveAttackCamera();
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

        PlayAttackAnimation();
    }

    private void PlayAttackAnimation()
    {
        if (useOwnerCharacterAnimator && ownerAnimator != null && !string.IsNullOrWhiteSpace(attackTriggerName))
        {
            ownerAnimator.ResetTrigger(attackTriggerName);
            ownerAnimator.SetTrigger(attackTriggerName);
            return;
        }

        if (_anim && attackAnimations != null && attackAnimations.Length > 0)
        {
            string anim = attackAnimations[attackIndex];
            _anim.CrossFadeInFixedTime(anim, 0.2f);
            attackIndex = (attackIndex + 1) % attackAnimations.Length;
            return;
        }

        if (_anim != null && !string.IsNullOrWhiteSpace(attackTriggerName))
        {
            _anim.ResetTrigger(attackTriggerName);
            _anim.SetTrigger(attackTriggerName);
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
        if (cam == null)
        {
            ResolveAttackCamera();
        }

        if (cam == null)
        {
            Debug.LogWarning("Melee attack failed because no camera was resolved for the weapon.", this);
            return;
        }

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
        if (audioSource != null) {        
            audioSource.pitch = 1;
        audioSource.PlayOneShot(hitSound); 
        }

        if (hitEffect != null)
        {
            GameObject GO = Instantiate(hitEffect, pos, Quaternion.identity);
            Destroy(GO, 20);
        }
    }

    private void ResolveAttackCamera()
    {
        cam = ownerCamera != null ? ownerCamera : Camera.main;
    }

}
