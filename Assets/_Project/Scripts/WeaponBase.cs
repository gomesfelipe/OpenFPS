using UnityEngine;
public enum WeaponType
{
    Melee,
    ShortRange,
    LongRange
}
public abstract class WeaponBase : MonoBehaviour, IWeapon
{
    public Animator _anim;
    [SerializeField] protected AudioSource audioSource;
    public AudioClip attackSound, hitSound;
    [SerializeField] protected string weaponName;
    [SerializeField] protected Sprite quickSlotIcon;
    [Header("Weapon settings")]
    public WeaponType weaponType;
    [Header("Equip Pose")]
    [SerializeField] private Vector3 equippedLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 equippedLocalEulerAngles = Vector3.zero;
    public WeaponType Type => weaponType;
    public string DisplayName => string.IsNullOrWhiteSpace(weaponName) ? name : weaponName;
    public Sprite QuickSlotIcon => quickSlotIcon;
    public Vector3 EquippedLocalPosition => equippedLocalPosition;
    public Vector3 EquippedLocalEulerAngles => equippedLocalEulerAngles;

    public float attackDistance = 3f, attackRange = 1.5f, attackRadius = 0.5f;
    public float attackDelay = 0.4f, attackSpeed = 1f;

    [SerializeField] public float attackDamage = 1f;
    [SerializeField] protected float fireRate;

    protected float lastShotTime;
    protected bool attacking = false,  readyToAttack = true;
    public LayerMask attackLayer;
    protected WeaponHandler owner;
    protected Animator ownerAnimator;
    protected Camera ownerCamera;

    public virtual bool CanFire => Time.time - lastShotTime >= fireRate;

    protected virtual void Start() { } 

    public abstract void Fire();

    public virtual void Reload() { }

    public virtual void OnAttackHitAnimationEvent() { }

    public virtual void OnAttackRecoveryAnimationEvent() { }

    public virtual void SetOwner(WeaponHandler character)
    {
        owner = character;
        ownerAnimator = character != null ? character.GetCharacterAnimator() : null;
        ownerCamera = character != null ? character.GetOwnerCamera() : null;
        _anim ??= GetComponentInChildren<Animator>(true);
        audioSource ??= GetComponentInChildren<AudioSource>(true);
        SetWeaponCollidersEnabled(character == null);
    }

    protected void SetWeaponCollidersEnabled(bool isEnabled)
    {
        var colliders = GetComponentsInChildren<Collider>(true);
        for (int index = 0; index < colliders.Length; index++)
        {
            var collider = colliders[index];
            if (collider == null)
            {
                continue;
            }

            collider.enabled = isEnabled;
        }
    }

    protected bool HasOwnerAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (ownerAnimator == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        var parameters = ownerAnimator.parameters;
        for (int index = 0; index < parameters.Length; index++)
        {
            if (parameters[index].name == parameterName && parameters[index].type == parameterType)
            {
                return true;
            }
        }

        return false;
    }

    protected void TriggerOwnerAnimatorIfAvailable(string triggerName)
    {
        if (!HasOwnerAnimatorParameter(triggerName, AnimatorControllerParameterType.Trigger))
        {
            return;
        }

        ownerAnimator.ResetTrigger(triggerName);
        ownerAnimator.SetTrigger(triggerName);
    }
}
