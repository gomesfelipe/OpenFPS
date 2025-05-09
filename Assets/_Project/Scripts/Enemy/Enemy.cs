using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyCharacter enemyCharacter;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Transform target;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;

    [SerializeField] private float lostTargetTimeout = 5f;
    private Vector3? lastKnownTargetPosition;
    private float timeSinceLostVisual = 0f;
    [SerializeField] private float attackDamage = 10f;
    private float lastAttackTime;

    private void Start()
    {
        enemyCharacter ??= GetComponent<EnemyCharacter>();
        enemyCharacter.Initialize();
        enemyHealth??=GetComponent<EnemyHealth>();
        enemyHealth?.Initialize();
        if (TryGetComponent<EnemyHealth>(out var health))
        {
            health.SetCamera(Camera.main.transform);
        }
    }
    private void Update()
    {
        if (target == null) return;

        Vector3 direction = (target.position - transform.position);
        Vector3 moveDir = new Vector2(direction.x, direction.z).normalized;
        Quaternion look = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z).normalized);

        var input = new CharacterInput
        {
            Move = moveDir,
            Rotation = look,
            Attack = Vector3.Distance(transform.position, target.position) <= attackRange
        };

        enemyCharacter.UpdateInput(input);

        if (input.Attack)
            TryAttack();
    }

    private void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;

        if (Physics.SphereCast(transform.position + Vector3.up, 0.5f, transform.forward, out RaycastHit hit, attackRange))
        {
            if (hit.collider.TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(attackDamage);
            }
        }
    }

    public void SetTarget(Transform newTarget) => target = newTarget;


}
