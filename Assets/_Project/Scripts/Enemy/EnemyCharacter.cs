using KinematicCharacterController;
using UnityEngine;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class EnemyCharacter : MonoBehaviour, ICharacterController
{
    [Header("References")]
    [SerializeField] private Animator _anim;
    [SerializeField] private Transform headTransform;

    [Header("Movement")]
    [SerializeField] private float airAcceleration = 30f;
    [SerializeField] private float maxAirSpeed = 6f;
    [SerializeField] private float gravity = -90f;

    [Header("Chase Behavior")]
    [SerializeField] private float baseSpeed = 4f;
    [SerializeField] private float boostedSpeed = 7f;
    [SerializeField] private float chaseBoostRange = 3f;

    [Header("Vision")]
    [SerializeField] private float visionRange = 10f;
    [SerializeField] private float visionAngle = 80f;
    [SerializeField] private LayerMask visionObstacles;

    private KinematicCharacterMotor motor;
    private CharacterInput _input;
    private CharacterState _state;

    private Transform _target;
    private Vector3 _requestedMovement;
    private Quaternion _requestedRotation;

    public void Initialize()
    {
        motor ??= GetComponent<KinematicCharacterMotor>();
        _anim ??= GetComponentInChildren<Animator>();
        motor.CharacterController = this;
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    public void UpdateInput(CharacterInput input)
    {
        _input = input;
        _requestedRotation = input.Rotation;
        _requestedMovement = new Vector3(input.Move.x, 0f, input.Move.y);
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);
        _requestedMovement = input.Rotation * _requestedMovement;
    }

    public CharacterState GetState() => _state;

    public void UpdateCharacter(float deltaTime)
    {
        if (_anim)
        {
            _anim.SetBool("IsGrounded", _state.Grounded);
            _anim.SetFloat("Speed", new Vector3(_state.velocity.x, 0f, _state.velocity.z).magnitude);
        }
    }

    public bool CanSeeTarget()
    {
        if (_target == null) return false;

        Vector3 dirToTarget = _target.position - headTransform.position;
        if (dirToTarget.magnitude > visionRange) return false;

        float angle = Vector3.Angle(transform.forward, dirToTarget);
        if (angle > visionAngle / 2f) return false;

        if (Physics.Raycast(headTransform.position, dirToTarget.normalized, out RaycastHit hit, visionRange, visionObstacles))
        {
            if (!hit.collider.transform.IsChildOf(_target))
                return false;
        }

        return true;
    }

    public void PlayAttack()
    {
        _anim?.SetTrigger("Attack");
    }
    public bool ShouldChaseTarget()
    {
        if (_target == null || headTransform == null) return false;

        Vector3 dirToTarget = _target.position - headTransform.position;

        if (Vector3.Angle(transform.forward, dirToTarget) > visionAngle * 0.5f)
            return false;

        if (dirToTarget.sqrMagnitude > visionRange * visionRange)
            return false;

        if (Physics.Raycast(headTransform.position, dirToTarget.normalized, out RaycastHit hit, visionRange, visionObstacles))
        {
            return hit.collider.GetComponentInParent<Player>() != null;
        }

        return false;
    }
    #region KinematicCharacterController

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _state.Acceleration = Vector3.zero;

        if (motor.GroundingStatus.IsStableOnGround)
        {
            _state.Grounded = true;

            float distanceToTarget = _target != null ? Vector3.Distance(transform.position, _target.position) : float.MaxValue;

            float t = Mathf.InverseLerp(visionRange, chaseBoostRange, distanceToTarget);
            float currentSpeed = Mathf.Lerp(baseSpeed, boostedSpeed, 1f - Mathf.Exp(-t * deltaTime));

            var groundMove = motor.GetDirectionTangentToSurface(_requestedMovement, motor.GroundingStatus.GroundNormal);
            var targetVelocity = groundMove * currentSpeed;

            _state.Acceleration = (targetVelocity - currentVelocity) / deltaTime;
            currentVelocity = targetVelocity;
        }
        else
        {
            _state.Grounded = false;

            // Aéreo
            Vector3 planarCurrent = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
            Vector3 movementForce = Vector3.ProjectOnPlane(_requestedMovement, motor.CharacterUp) * airAcceleration * deltaTime;

            if (planarCurrent.magnitude < maxAirSpeed)
            {
                var target = Vector3.ClampMagnitude(planarCurrent + movementForce, maxAirSpeed);
                movementForce = target - planarCurrent;
            }

            currentVelocity += movementForce;
            currentVelocity += gravity * deltaTime * motor.CharacterUp;
        }

        _state.velocity = currentVelocity;
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        var forward = Vector3.ProjectOnPlane(_requestedRotation * Vector3.forward, motor.CharacterUp);
        if (forward != Vector3.zero)
        {
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
        }
    }

    public void BeforeCharacterUpdate(float deltaTime) { }
    public void AfterCharacterUpdate(float deltaTime)
    {
        _state.velocity = motor.Velocity;
        _state.Grounded = motor.GroundingStatus.IsStableOnGround;

        if (_anim)
        {
            _anim.SetBool("IsGrounded", _state.Grounded);
            _anim.SetFloat("Speed", new Vector3(_state.velocity.x, 0f, _state.velocity.z).magnitude);
        }
    }
    public void PostGroundingUpdate(float deltaTime) { }

    public bool IsColliderValidForCollisions(Collider coll) => true;
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport report) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport report) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 pos, Quaternion rot, ref HitStabilityReport report) { }

    #endregion
}
