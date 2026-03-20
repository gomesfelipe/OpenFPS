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
        enemyHealth ??= GetComponent<EnemyHealth>();
        enemyHealth?.Initialize();
        if (TryGetComponent<EnemyHealth>(out var health))
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                health.SetCamera(mainCamera.transform);
            }
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

        ChaseTarget();
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
        if (target == null)
        {
            return;
        }

        CharacterInput input = BuildInput(target.position, true);
        ApplyInput(input);

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

        CharacterInput input = BuildInput(patrolDestination.Value, false);
        ApplyInput(input);
    }

    private CharacterInput BuildInput(Vector3 destination, bool allowAttack)
    {
        Vector3 planarDirection = destination - transform.position;
        planarDirection.y = 0f;

        Vector2 move = planarDirection.sqrMagnitude > 0.0001f
            ? new Vector2(planarDirection.x, planarDirection.z).normalized
            : Vector2.zero;

        Vector3 forward = planarDirection.sqrMagnitude > 0.0001f
            ? planarDirection.normalized
            : transform.forward;

        return new CharacterInput
        {
            Move = move,
            Rotation = Quaternion.LookRotation(forward),
            Attack = allowAttack && planarDirection.sqrMagnitude <= attackRange * attackRange
        };
    }

    private void ApplyInput(CharacterInput input)
    {
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
