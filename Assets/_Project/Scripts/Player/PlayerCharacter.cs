using KinematicCharacterController;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public enum CrouchInput
{
    None, Toggle
}
public enum Stance
{
    Stand, Crouch, Slide
}
public struct CharacterState
{
    public bool Grounded;
    public Stance Stance;
    public Vector3 velocity;
    public Vector3 Acceleration;
}
public struct CharacterInput
{
    public Quaternion Rotation;
    public Vector2 Move;
    public bool Jump;
    public bool JumpSustain;
    public bool Interact;
    public CrouchInput Crouch;
    public bool Attack, AttackSustain;
    public bool Reload;
    public bool Aim;
}

[RequireComponent(typeof(KinematicCharacterMotor))]
public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Animator _anim;
    [SerializeField] private Transform root, cameraTarget;
    [Space]
    [SerializeField] private float walkSpeed=20f, jumpSpeed=20f, crouchSpeed=7f;
    [SerializeField] private float walkResponse = 25f, crouchResponse=20f;
    [Space]
    [SerializeField] private float airSpeed = 15f, airAcceleration =70f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [Space]
    [SerializeField,Range(0,1f)] private float jumpSustainGravity = 0.4f;
    [SerializeField] private float gravity = -90f;
    [SerializeField] private float fallGravityMultiplier = 1.2f;
    [SerializeField] private float jumpReleaseGravityMultiplier = 1.5f;
    [Space]
    [SerializeField] private float slideStartSpeed = 25f, slideEndSpeed=15f;
    [SerializeField] private float slideFriction = 0.8f, slideSteerAcceleration = 5f;
    [SerializeField] private float slideGravity = -90f;
    [Space]
    [SerializeField] private float standHeight = 2f, crouchHeight = 1f;
    [SerializeField, Range(0f,1f)] private float standCameraTargetHeight = 0.9f, crouchCameraTargetHeight = 0.7f;
    [SerializeField] private float crouchHeightResponse = 15f;
    [Header("Aim Rig")]
    [SerializeField] private bool enableAimRig = true;
    [SerializeField] private float aimRigDistance = 12f;
    [SerializeField] private float aimRigWeightResponse = 12f;
    [SerializeField, Range(0f, 1f)] private float upperChestAimWeight = 0.8f;
    [SerializeField, Range(0f, 1f)] private float headAimWeight = 0.35f;
    [SerializeField] private string aimRigName = "Rig 1";
    [SerializeField] private string manualChestAimConstraintName = "ChestAim";
    [SerializeField] private string manualAimTargetName = "aimtarget";
    private CharacterState _state, _lastState, _tempState;
    [Space]
    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private bool _requestedJump, _requestedCrouch, _requestedSustainedJump, _requestedCrouchInAir;
    private bool _requestedAim;
    private float _timeSinceUngrounded, _timeSinceJumpRequest;
    private bool _ungroundedDueToJump;
    private Collider[] _uncrouchOverlapResults;
    private bool _isInitialized;
    private float _currentCameraTargetHeight;
    private bool _hasIsAimingParameter;
    private PlayerCamera _playerCamera;
    private WeaponHandler _weaponHandler;
    private RigBuilder _rigBuilder;
    private Rig _aimRig;
    private Transform _aimRigTarget;
    private MultiAimConstraint _upperChestAimConstraint;
    private MultiAimConstraint _headAimConstraint;

    private void Awake()
    {
        motor ??= GetComponent<KinematicCharacterMotor>();
        _anim ??= GetComponentInChildren<Animator>();
        root ??= transform;
        _currentCameraTargetHeight = standHeight * standCameraTargetHeight;

        if (motor != null)
        {
            motor.CharacterController = this;
        }
    }

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        motor ??= GetComponent<KinematicCharacterMotor>();
        _anim ??= GetComponentInChildren<Animator>();
        _state.Stance = Stance.Stand;
        _lastState = _state;
        root ??= transform;
        _uncrouchOverlapResults ??= new Collider[8];
        _currentCameraTargetHeight = standHeight * standCameraTargetHeight;
        _hasIsAimingParameter = AnimatorHasParameter(_anim, "IsAiming");
        _playerCamera ??= GetComponentInChildren<PlayerCamera>(true);
        _weaponHandler ??= GetComponent<WeaponHandler>();

        if (enableAimRig)
        {
            EnsureAimRig();
        }

        if (motor != null)
        {
            motor.CharacterController = this;
        }

        _isInitialized = true;
    }
    public void UpdateBody(float deltaTime)
    {
        var currentHeight = motor.Capsule.height;
        var targetCameraTargetHeight = currentHeight *
            (
            _state.Stance is Stance.Stand
            ? standCameraTargetHeight
            : crouchCameraTargetHeight
            );
        _currentCameraTargetHeight = Mathf.Lerp(
            _currentCameraTargetHeight,
            targetCameraTargetHeight,
            1f - Mathf.Exp(-crouchHeightResponse * deltaTime));
        cameraTarget.position = transform.position + transform.up * _currentCameraTargetHeight;

        // Updating the model Animator
        if (_anim != null)
        {
            _anim.SetBool("IsGrounded", _state.Grounded);
            _anim.SetBool("IsCrouching", _state.Stance is Stance.Crouch);
            _anim.SetFloat("Speed", new Vector3(_state.velocity.x, 0f, _state.velocity.z).magnitude);

            if (_hasIsAimingParameter)
            {
                _anim.SetBool("IsAiming", _requestedAim);
            }
        }

        UpdateAimRig(deltaTime);

    }
    public void UpdateInput(CharacterInput input)
    {
        _requestedRotation = input.Rotation;
        //Take the 2D input vector3 and create a 3D mivenebt vector3 in the XZ plane.
        _requestedMovement = new Vector3(input.Move.x,0, input.Move.y);
        //Clamp the length to 1 to prevent moving faster diagonally with WASD input
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);
        //Orient the input so it's relative to the direction the player is facing.
        _requestedMovement = input.Rotation * _requestedMovement;
        var wasRequestingJump = _requestedJump;
        _requestedJump = _requestedJump || input.Jump;
        if (_requestedJump && !wasRequestingJump) 
        {
            _timeSinceJumpRequest = 0f;
        }
        _requestedSustainedJump = input.JumpSustain;
        var wasRequestingCrouch = _requestedCrouch;
        _requestedCrouch = input.Crouch switch
        {
            CrouchInput.Toggle => !_requestedCrouch,
            CrouchInput.None => _requestedCrouch,
            _=>_requestedCrouch
        };
        if(_requestedCrouch && !wasRequestingCrouch)
        {
            _requestedCrouchInAir = !_state.Grounded;
        }
        else if(!_requestedCrouch && wasRequestingCrouch){
            _requestedCrouch = false;
        }

        _requestedAim = input.Aim;
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _state.Acceleration = Vector3.zero;
        //If on the ground...
        if (motor.GroundingStatus.IsStableOnGround)
        {
            _timeSinceUngrounded = 0f;
            _ungroundedDueToJump = false;

            /* Snap the requested movement direction to the angle of the surface 
             * the character is currently walking on. */
            var groundMovement = motor.GetDirectionTangentToSurface
              (
              direction: _requestedMovement,
              surfaceNormal: motor.GroundingStatus.GroundNormal
              ) * _requestedMovement.magnitude;
            //Start Sliding.
            {
                var moving = groundMovement.sqrMagnitude > 0f;
                var crouching = _state.Stance is Stance.Crouch;
                var wasStanding = _lastState.Stance is Stance.Stand;
                var wasInAir = !_lastState.Grounded;
                if (moving && crouching && (wasStanding || wasInAir))
                {
                    _state.Stance = Stance.Slide;
                    /* When landing on stable ground the character motor projects the velocity onto a flat ground plane.
                     * See: KinematicCharacterMotor.HandleVelocityProjection()
                     * This is normally good, because under normal circumstances the player shouldn't slide when landing on the ground.
                     * In this case we *want* the player to slide.
                     * Reproject the last frames (falling) velocity onto the ground normal to slide.
                     */
                    if (wasInAir)
                    {
                        currentVelocity = Vector3.ProjectOnPlane
                            (
                            vector: _lastState.velocity,
                            planeNormal: motor.GroundingStatus.GroundNormal
                            );
                    }
                    //Debug.DrawRay(transform.position,currentVelocity, Color.red,5f);
                    //Debug.DrawRay(transform.position, _lastState.velocity, Color.green, 5f);
                   var effectiveSlideStartSpeed = slideStartSpeed;
                    if (!_lastState.Grounded && !_requestedCrouchInAir) {
                        effectiveSlideStartSpeed = 0f;
                        _requestedCrouchInAir = false;
                    }
                    var slideSpeed = Mathf.Max(effectiveSlideStartSpeed, currentVelocity.magnitude);
                    currentVelocity = motor.GetDirectionTangentToSurface
                        (
                        direction: currentVelocity,
                        surfaceNormal: motor.GroundingStatus.GroundNormal
                        ) * slideSpeed;
                    Debug.DrawRay(transform.position, currentVelocity, Color.green,5f);

                }
            }
            //Move
            if(_state.Stance is Stance.Stand or Stance.Crouch)
            {            

                /* Calculate the speed and responsiveness of movement 
                 *  on the character's stance. */
                var speed = _state.Stance is Stance.Stand ? walkSpeed : crouchSpeed;
                var response = _state.Stance is Stance.Stand
                    ? walkResponse
                    : crouchResponse;
                // and smoothly move along the ground in that direction.
                var targetVelocity = groundMovement * speed;
                var moveVelocity = Vector3.Lerp(
                    a: currentVelocity,
                    b: targetVelocity,
                    t: 1f - Mathf.Exp(-response * deltaTime)
                    );
                _state.Acceleration = moveVelocity - currentVelocity;
                currentVelocity = moveVelocity;
            }
            else
            {
                //Friction.
                currentVelocity -= currentVelocity * (slideFriction * deltaTime);
                //Slope
                {
                    var force = Vector3.ProjectOnPlane
                        (
                        vector: -motor.CharacterUp,
                        planeNormal: motor.GroundingStatus.GroundNormal
                        ) * slideGravity;
                    currentVelocity -= force * deltaTime;
                }
                //Steer
                {
                    //Target velocity is the player's movement direction, at the current speed.
                    var currentSpeed = currentVelocity.magnitude;
                    var targetVelocity = groundMovement * currentSpeed;
                    var steerVelocity = currentVelocity;
                    var steerForce = deltaTime * slideSteerAcceleration * (targetVelocity - currentVelocity);
                    //Add steer force but clamp velocity so the slide doesn't increase due to direct movement.
                    steerVelocity += steerForce;
                    steerVelocity = Vector3.ClampMagnitude(currentVelocity,currentSpeed);
                    _state.Acceleration = (steerVelocity - currentVelocity) / deltaTime;
                    currentVelocity = steerVelocity;
                }

                //Stop.
                if (currentVelocity.magnitude < slideEndSpeed)
                {
                    _state.Stance = Stance.Crouch;
                }
            }
        }
        // else, in the air...
        else
        {
            _timeSinceUngrounded += deltaTime;
            //Move
            if (_requestedMovement.sqrMagnitude > 0f) 
            {
                //Requested movement project onto movement plane. (magnitude preserved)
                var planarMovement = Vector3.ProjectOnPlane
                    (
                    vector:_requestedMovement,
                    planeNormal:motor.CharacterUp
                    )*_requestedMovement.magnitude;
                //Current veloctiy on movement plane.
                var currentPlanarVelocity = Vector3.ProjectOnPlane
                    (
                    vector: currentVelocity,
                    planeNormal: motor.CharacterUp
                    );
                //Calculate movement force.
                //Will be changed depending on current velocity.
                var movementForce = planarMovement * airAcceleration * deltaTime;
                //If moving slower than the max air speed, treat movementForce as a simple steering force.
                if (currentPlanarVelocity.magnitude < airSpeed)
                {
                //Add it to the current planar velocity for a target velocity.
                var targetPlanarVelocity = currentPlanarVelocity + movementForce;
                //Limit target velocity to air spedd;
                targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, airSpeed);
                    //Steer towards target velocity.
                    movementForce = targetPlanarVelocity - currentPlanarVelocity;
                }
                //Otherwise, nerf the movement force when it's in the direction of the current velocity
                //to prevent accelerating further beyond the max air speed.
                //Steer toward current velocity.
                else if(Vector3.Dot(currentPlanarVelocity,movementForce)>0f)
                {
                    //Project movement force onti the plane whose normal is the current planar velocity
                    var constrainedMovementForce = Vector3.ProjectOnPlane
                        (
                        vector: movementForce, 
                        planeNormal: currentPlanarVelocity.normalized
                        );
                    movementForce = constrainedMovementForce;
                }
                //Prevent air-climbing steep slopes.
                if(motor.GroundingStatus.FoundAnyGround)
                {
                    //If moving the same direction as the resultant velocity...
                    if (Vector3.Dot(movementForce, currentVelocity + movementForce) > 0f)
                    {
                        //Calculate obstruction normal.
                        var obstructionNormal = Vector3.Cross
                            (
                            motor.CharacterUp,
                            motor.GroundingStatus.GroundNormal
                            ).normalized;
                        //Project movement force onto obstruction plane.
                        movementForce = Vector3.ProjectOnPlane(movementForce,obstructionNormal);
                    }
                }
                    currentPlanarVelocity += movementForce;
            }
            //Gravity
            var effectiveGravity = gravity;
            var verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            if (_requestedSustainedJump && verticalSpeed > 0f)
            {
                effectiveGravity *= jumpSustainGravity;
            }
            else if (verticalSpeed > 0f)
            {
                effectiveGravity *= jumpReleaseGravityMultiplier;
            }
            else if (verticalSpeed < 0f)
            {
                effectiveGravity *= fallGravityMultiplier;
            }

            currentVelocity += deltaTime * effectiveGravity * motor.CharacterUp;
        }

        if (_requestedJump)
        {
            var grounded = motor.GroundingStatus.IsStableOnGround;
            var canCoyoteJump = _timeSinceUngrounded < coyoteTime && !_ungroundedDueToJump;
            if (grounded || canCoyoteJump) 
            {
                _requestedJump = false; //Unset jump request
                _timeSinceJumpRequest = 0f;
                _requestedCrouch = false; //and request the character uncrouches.
                _requestedCrouchInAir = false;
                //Unstick the player from the ground
                motor.ForceUnground(time: 0f);
                _ungroundedDueToJump = true;
                //Set minimum vertical to the jump speed.
                var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);
                //Add the difference in current and target vertical speed to the character's
                currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
            }
            else
            {
                _timeSinceJumpRequest += deltaTime;
                //Defer the jump request long enough to catch a near-future landing.
                var canJumpLater = _timeSinceJumpRequest < jumpBufferTime;
                _requestedJump = canJumpLater;
            }

        }
        else
        {
            _requestedJump = false; 
        }
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        /* Update the  character's rotation to face in the same direction as the 
         requested rotation (camera rotation).
         We don't want the character to pitch up and down, so the direction the character 
        looks should always be "flattened".
        This is done by projectin a vector pointing in the same direction that 
        the player is looking onto a flat ground plane.*/
        var forward = Vector3.ProjectOnPlane
        (
            _requestedRotation * Vector3.forward,motor.CharacterUp
        );
        if(forward!= Vector3.zero)
        {
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
        }
        
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        _tempState = _state;
       //Crouch 
       if(_requestedCrouch && _state.Stance is Stance.Stand)
        {
            _state.Stance = Stance.Crouch;
            motor.SetCapsuleDimensions
                (
                radius: motor.Capsule.radius,
                height: crouchHeight,
                yOffset:crouchHeight*0.5f
                );
        }
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        if (!motor.GroundingStatus.IsStableOnGround && _state.Stance is Stance.Slide) {
            _state.Stance = Stance.Crouch;
        }
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        //Uncrouch
        if (!_requestedCrouch && _state.Stance is not Stance.Stand)
        {
            //Tentatively "standup" the character capsule.
            _state.Stance = Stance.Stand;
            motor.SetCapsuleDimensions
                (
                radius: motor.Capsule.radius,
                height: standHeight,
                yOffset: standHeight * 0.5f
                );

            //Then see if the capsule overlaps any colliders before actually
            // allowing the characte to standup
            var pos = motor.TransientPosition;
            var rot = motor.TransientRotation;
            var mask = motor.CollidableLayers;
            if(motor.CharacterOverlap(pos, rot,_uncrouchOverlapResults, mask, QueryTriggerInteraction.Ignore)>0)
            {
                //Re-crouch.
                _requestedCrouch = true;
                motor.SetCapsuleDimensions
                (
                radius: motor.Capsule.radius,
                height: crouchHeight,
                yOffset: crouchHeight * 0.5f
                );
            }
            else
            {
                _state.Stance = Stance.Stand;
            }
        }
        //Update state to reflect relevant motor properties.
        _state.Grounded = motor.GroundingStatus.IsStableOnGround;
        _state.velocity = motor.Velocity;
        //And update the _lastState to store the character state snapshot taken at the beginning of this character update
        _lastState = _tempState;
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {

        return coll != motor.Capsule;
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
      
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {

    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
        
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
        throw new System.NotImplementedException();
    }

    public Transform GetCameraTarget() => cameraTarget;
    public Animator GetAnimator() => _anim;
    public CharacterState GetState() => _state;
    public CharacterState GetLastState() => _lastState;
    public Vector3 GetRequestedMovement() => _requestedMovement;
    public void SetPosition(Vector3 position, bool killVelocity=true)
    {
        motor.SetPosition(position);
        if (killVelocity)
        {
            motor.BaseVelocity = Vector3.zero;
        }
    }

    private static bool AnimatorHasParameter(Animator animator, string parameterName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        foreach (var parameter in animator.parameters)
        {
            if (parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureAimRig()
    {
        if (_anim == null)
        {
            return;
        }

        _rigBuilder ??= _anim.GetComponent<RigBuilder>();
        if (_rigBuilder == null)
        {
            _rigBuilder = _anim.gameObject.AddComponent<RigBuilder>();
        }

        _aimRig ??= FindOrCreateAimRig();
        _aimRigTarget ??= FindOrCreateAimRigTarget();

        if (_aimRig == null || _aimRigTarget == null)
        {
            return;
        }

        var upperChest = ResolveAimBone(HumanBodyBones.UpperChest, HumanBodyBones.Chest, HumanBodyBones.Spine);
        var head = ResolveAimBone(HumanBodyBones.Head, HumanBodyBones.Neck);

        if (upperChest != null)
        {
            _upperChestAimConstraint = FindOrCreateMultiAimConstraint("UpperChest Aim", upperChest, upperChestAimWeight);
        }

        if (head != null)
        {
            _headAimConstraint = FindOrCreateMultiAimConstraint("Head Aim", head, headAimWeight);
        }

        RebuildRigGraph();
    }

    private void UpdateAimRig(float deltaTime)
    {
        if (!enableAimRig || _aimRig == null)
        {
            return;
        }

        UpdateAimRigTargetTransform();

        bool shouldAim = _requestedAim && _weaponHandler != null && _weaponHandler.CanAim;
        float targetWeight = shouldAim ? 1f : 0f;
        float blend = 1f - Mathf.Exp(-aimRigWeightResponse * deltaTime);
        _aimRig.weight = Mathf.Lerp(_aimRig.weight, targetWeight, blend);
    }

    public void AnimationEvent_AttackHit()
    {
        _weaponHandler ??= GetComponent<WeaponHandler>();
        _weaponHandler?.HandleAttackHitAnimationEvent();
    }

    public void AnimationEvent_AttackRecover()
    {
        _weaponHandler ??= GetComponent<WeaponHandler>();
        _weaponHandler?.HandleAttackRecoveryAnimationEvent();
    }

    private Rig FindOrCreateAimRig()
    {
        var rigs = GetComponentsInChildren<Rig>(true);
        for (int index = 0; index < rigs.Length; index++)
        {
            if (rigs[index] != null && rigs[index].name == aimRigName)
            {
                rigs[index].weight = 0f;
                return rigs[index];
            }
        }

        var rigObject = new GameObject("Aim Rig");
        var rigTransform = rigObject.transform;
        rigTransform.SetParent(root != null ? root : _anim.transform, false);
        var rig = rigObject.AddComponent<Rig>();
        rig.weight = 0f;
        return rig;
    }

    private Transform FindOrCreateAimRigTarget()
    {
        _playerCamera ??= GetComponentInChildren<PlayerCamera>(true);
        if (_playerCamera == null)
        {
            return null;
        }

        var existingTarget = FindNamedChildTransform(manualAimTargetName);
        existingTarget ??= FindNamedChildTransform("Aim Rig Target");
        if (existingTarget != null)
        {
            UpdateAimRigTargetTransform(existingTarget);
            return existingTarget;
        }

        var targetObject = new GameObject("Aim Rig Target");
        var targetTransform = targetObject.transform;
        targetTransform.SetParent(_playerCamera.transform, false);
        UpdateAimRigTargetTransform(targetTransform);
        return targetTransform;
    }

    private Transform ResolveAimBone(params HumanBodyBones[] candidates)
    {
        if (_anim == null || !_anim.isHuman)
        {
            return null;
        }

        for (int index = 0; index < candidates.Length; index++)
        {
            var bone = _anim.GetBoneTransform(candidates[index]);
            if (bone != null)
            {
                return bone;
            }
        }

        return null;
    }

    private MultiAimConstraint FindOrCreateMultiAimConstraint(string constraintName, Transform constrainedObject, float constraintWeight)
    {
        var constraintTransform = _aimRig.transform.Find(manualChestAimConstraintName);
        if (constraintTransform == null)
        {
            constraintTransform = _aimRig.transform.Find(constraintName);
        }
        if (constraintTransform == null)
        {
            var constraintObject = new GameObject(constraintName);
            constraintTransform = constraintObject.transform;
            constraintTransform.SetParent(_aimRig.transform, false);
        }

        var constraint = constraintTransform.GetComponent<MultiAimConstraint>();
        bool createdConstraint = false;
        if (constraint == null)
        {
            constraint = constraintTransform.gameObject.AddComponent<MultiAimConstraint>();
            constraint.Reset();
            createdConstraint = true;
        }

        constraint.weight = constraintWeight;

        if (createdConstraint || !constraint.IsValid())
        {
            ref var data = ref constraint.data;
            data.constrainedObject = constrainedObject;
            var sourceObjects = new WeightedTransformArray(1);
            sourceObjects.Add(new WeightedTransform(_aimRigTarget, 1f));
            data.sourceObjects = sourceObjects;
            data.maintainOffset = true;
            data.offset = Vector3.zero;
            data.aimAxis = MultiAimConstraintData.Axis.Z;
            data.upAxis = MultiAimConstraintData.Axis.Y;
            data.worldUpType = MultiAimConstraintData.WorldUpType.SceneUp;
            data.worldUpAxis = MultiAimConstraintData.Axis.Y;
            data.worldUpObject = null;
            data.constrainedXAxis = true;
            data.constrainedYAxis = true;
            data.constrainedZAxis = true;
            data.limits = new Vector2(-180f, 180f);
        }

        return constraint;
    }

    private Transform FindNamedChildTransform(string transformName)
    {
        if (string.IsNullOrWhiteSpace(transformName))
        {
            return null;
        }

        var transforms = GetComponentsInChildren<Transform>(true);
        for (int index = 0; index < transforms.Length; index++)
        {
            if (transforms[index] != null && transforms[index].name == transformName)
            {
                return transforms[index];
            }
        }

        return null;
    }

    private void UpdateAimRigTargetTransform()
    {
        UpdateAimRigTargetTransform(_aimRigTarget);
    }

    private void UpdateAimRigTargetTransform(Transform targetTransform)
    {
        if (targetTransform == null)
        {
            return;
        }

        _playerCamera ??= GetComponentInChildren<PlayerCamera>(true);
        if (_playerCamera == null)
        {
            return;
        }

        var cameraTransform = _playerCamera.transform;
        if (targetTransform.parent == cameraTransform)
        {
            targetTransform.localPosition = new Vector3(0f, 0f, aimRigDistance);
            targetTransform.localRotation = Quaternion.identity;
            return;
        }

        targetTransform.position = cameraTransform.position + cameraTransform.forward * aimRigDistance;
        targetTransform.rotation = cameraTransform.rotation;
    }

    private void RebuildRigGraph()
    {
        if (_rigBuilder == null || _aimRig == null)
        {
            return;
        }

        bool hasConstraints = (_upperChestAimConstraint != null && _upperChestAimConstraint.IsValid()) ||
            (_headAimConstraint != null && _headAimConstraint.IsValid());
        if (!hasConstraints)
        {
            return;
        }

        _rigBuilder.Clear();

        var layers = _rigBuilder.layers;
        bool hasAimRigLayer = false;
        for (int index = 0; index < layers.Count; index++)
        {
            if (layers[index] != null && layers[index].rig == _aimRig)
            {
                hasAimRigLayer = true;
                layers[index].active = true;
                break;
            }
        }

        if (!hasAimRigLayer)
        {
            layers.Add(new RigLayer(_aimRig));
        }

        _rigBuilder.Build();
    }


}
