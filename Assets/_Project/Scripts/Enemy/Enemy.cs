using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyCharacter enemyCharacter;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Transform target;
    [SerializeField] private float detectionRadius = 20f;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;

    [SerializeField] private float lostTargetTimeout = 5f;
    private Vector3? lastKnownTargetPosition;
    private float timeSinceLostVisual = 0f;
    [SerializeField] private float attackDamage = 10f;
    private float lastAttackTime;

    public enum PatrolMode { None, Waypoints, Random }

    [Header("Patrol")]
    [SerializeField] private PatrolMode patrolMode = PatrolMode.Waypoints;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolPointRadius = 1.5f;
    [SerializeField] private float randomPatrolRadius = 5f;
    [SerializeField] private float idleTimeAtPoint = 2f;

    private int currentPatrolIndex = 0;
    private float patrolIdleTimer = 0f;
    private Vector3? patrolDestination;

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
        if (target == null)
        {
            TryFindTarget();

            if (target == null)
            {
                Patrol();
                return;
            }
        }

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
        enemyCharacter.UpdateCharacter(Time.deltaTime);

        if (input.Attack)
            TryAttack();
    }

    private void TryFindTarget()
    {
        Collider[] players = Physics.OverlapSphere(transform.position, detectionRadius, playerMask);

        foreach (var col in players)
        {
            Transform candidate = col.transform;

            if (!candidate.CompareTag(playerTag)) continue;

            Vector3 dirToTarget = (candidate.position - transform.position).normalized;

            if (Physics.Raycast(transform.position + Vector3.up, dirToTarget, out RaycastHit hit, detectionRadius))
            {
                if (hit.collider.transform == candidate || hit.collider.transform.IsChildOf(candidate))
                {
                    SetTarget(candidate);
                    break;
                }
            }
        }
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

    private void ChaseTarget()
    {
        Vector3 dir = target.position - transform.position;
        Vector2 move = new Vector2(dir.x, dir.z).normalized;
        Quaternion look = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z).normalized);

        var input = new CharacterInput
        {
            Move = move,
            Rotation = look,
            Attack = dir.magnitude <= attackRange
        };

        enemyCharacter.UpdateInput(input);
        enemyCharacter.UpdateCharacter(Time.deltaTime);

        if (input.Attack)
        {
            TryAttack();
            enemyCharacter.PlayAttack();
        }
    }

    private void Patrol()
    {
        if (!patrolDestination.HasValue || Vector3.Distance(transform.position, patrolDestination.Value) < patrolPointRadius)
        {
            patrolIdleTimer += Time.deltaTime;
            if (patrolIdleTimer < idleTimeAtPoint) return;

            patrolIdleTimer = 0f;
            patrolDestination = GetNextPatrolPoint();
        }

        Vector3 dir = patrolDestination.Value - transform.position;
        Vector2 move = new Vector2(dir.x, dir.z).normalized;
        Quaternion look = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z).normalized);

        var input = new CharacterInput
        {
            Move = move,
            Rotation = look,
            Attack = false
        };

        enemyCharacter.UpdateInput(input);
        enemyCharacter.UpdateCharacter(Time.deltaTime);
    }

    private Vector3 GetNextPatrolPoint()
    {
        switch (patrolMode)
        {
            case PatrolMode.Waypoints:
                if (patrolPoints.Length == 0) return transform.position;
                var point = patrolPoints[currentPatrolIndex % patrolPoints.Length].position;
                currentPatrolIndex++;
                return point;

            case PatrolMode.Random:
                Vector2 offset = Random.insideUnitCircle * randomPatrolRadius;
                Vector3 randomPoint = transform.position + new Vector3(offset.x, 0, offset.y);
                return randomPoint;

            default:
                return transform.position;
        }
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up, target.position + Vector3.up);
        }
    }
}
