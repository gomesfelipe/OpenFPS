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
    [Header("Weapon settings")]
    public WeaponType weaponType;
    public WeaponType Type => weaponType;

    public float attackDistance = 3f, attackRange = 1.5f, attackRadius = 0.5f;
    public float attackDelay = 0.4f, attackSpeed = 1f;

    [SerializeField] public float attackDamage = 1f;
    [SerializeField] protected float fireRate;

    protected float lastShotTime;
    protected bool attacking = false,  readyToAttack = true;
    public LayerMask attackLayer;
    protected WeaponHandler owner;

    public virtual bool CanFire => Time.time - lastShotTime >= fireRate;

    protected virtual void Start() { } 

    public abstract void Fire();

    public virtual void Reload() { }

    public virtual void SetOwner(WeaponHandler character)
    {
        owner = character;
    }
}
