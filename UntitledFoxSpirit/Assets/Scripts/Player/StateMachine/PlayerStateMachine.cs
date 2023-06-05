﻿using Cinemachine;
using NUnit.Framework.Internal;
using PathCreation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[SelectionBase]
public class PlayerStateMachine : MonoBehaviour
{
    public string debugState;
    public string subState;

    [HideInInspector]
    public VariableScriptObject vso;

    public PlayerBaseState currentState;
    private PlayerStateFactory states;

    [Header("References")]
    public Transform mainCamera;
    public Transform wallCheck;
    public Transform ledgeCheck;
    public Transform ledgeRootJntTransform;
    public PathCreator pathCreator;
    public Slider staminaBar;
    public CinemachineVirtualCamera virtualCam2D;
    public PhysicMaterial friction;

    #region Movement

    // Movement
    [HideInInspector] public float accelRatePerSec, decelRatePerSec, turnSmoothVelocity, currentSpeed, maxSpeed;
    [HideInInspector] public bool isAccel = false, isDecel = false, isRunning = false, disableMovement;


    // Rotation
    [HideInInspector] public float dampedTargetRotationCurrentYVelocity, dampedTargetRotationPassedTime;
    [HideInInspector] public bool disableUpdateRotations = false, disableInputRotations = false;
    [HideInInspector] public Quaternion previousRotation, targetRot2D;

    // Gravity
    [HideInInspector] public bool reduceVelocityOnce = false, disableGravity = false, isGrounded;
    [HideInInspector] public float updateMaxHeight = 100000f, updateMaxHeight2 = 100000f;

    #endregion

    #region Actions

    // Sneaking
    [HideInInspector] public bool canUnsneak = true;
    [HideInInspector] public bool animIsSneaking
    {
        get { return animController.GetBool("isSneaking"); }
        set { animController.SetBool("isSneaking", value); }
    }

    // Jump
    [HideInInspector] public bool isLanding = false, isLandRolling = false, disableJumping = false, canDoubleJump;
    [HideInInspector] public float newGroundY = 1000000f, jumpCounter, jumpBufferCounter, jumpCoyoteCounter;
    // Dash
    [HideInInspector] public float currentDashCooldown = 1f;
    [HideInInspector] bool disableDashing = false;
    [HideInInspector] public bool animIsDashing
    {
        get { return animController.GetBool("Dash"); }
    }
    [HideInInspector] public bool animIsRunning
    {
        get { return animController.GetBool("isSprinting"); }
    }

    // Ledge Climb
    [HideInInspector] public float currentLedgeHangCooldown;
    [HideInInspector] public bool isClimbing = false, isWallClimbing, canClimbWall, isTouchingWall, isTouchingLedge, canClimbLedge, ledgeDetected;

    // Attack
    [HideInInspector] public bool isAttacking = false, resetAttack  = true;
    [HideInInspector] public float currentAttackCooldown;
    [HideInInspector] public int comboCounter, lastAttackInt;

    #endregion

    #region Other

    // Stamina
    [HideInInspector] public float currentStaminaCooldown, currentStamina;

    // Take Damage
    [HideInInspector] public bool isInvulnerable = false;
    [HideInInspector] public float currentInvulnerableCooldown;

    // Path
    [HideInInspector] public float distanceOnPath;

    // References
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Animator animController;
    [HideInInspector] public CapsuleCollider tallCollider;
    [HideInInspector] public CapsuleCollider shortCollider;
    [HideInInspector] public BoxCollider boxCollider;
    [HideInInspector] public PlayerInput input;

    #endregion

    #region Internal Variables

    [HideInInspector] public Vector3 prevInputDirection;
    [HideInInspector] public bool isHeavyLand = false;

    // Debug
    [HideInInspector] public float currentMaxHeight = 0f;
    [HideInInspector] public Vector3 velocity;

    #endregion

    #region Unity Functions

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animController = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider>();
        input = GetComponent<PlayerInput>();

        CapsuleCollider[] colliderArr = GetComponentsInChildren<CapsuleCollider>();
        tallCollider = colliderArr[0];
        shortCollider = colliderArr[1];

        currentStamina = vso.maxStamina;

        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;


        Vector3 spawnPos = pathCreator.path.GetPointAtDistance(vso.distanceSpawn);
        spawnPos.y += vso.spawnYOffset + 1.0f;
        transform.position = spawnPos;
        distanceOnPath = pathCreator.path.GetClosestDistanceAlongPath(transform.position);


        states = new PlayerStateFactory(this, vso);
        currentState = new PlayerGroundedState(this, states, vso);
        currentState.EnterState();
    }

    // Update is called once per frame
    void Update()
    {
        if (rb.velocity.y > 0)
        {
            currentMaxHeight = transform.position.y;
        }

        currentState.UpdateStates();
        if (currentState.currentSubState != null)
        {
            subState = currentState.currentSubState.ToString();
        }
        debugState = currentState.ToString();
        //Debug.Log(subState);

        StoreInputMovement();

        // Rotation
        HandleRotation();
        CameraRotation();

        // Jump
        JumpCooldownTimer();
        CoytoteTime();

        // Dash
        DashCooldown();

        // Ground
        GroundCheck();
    }

    void FixedUpdate()
    {
        currentState.FixedUpdateStates();
    }

    void OnAnimatorMove()
    {
        if (isClimbing && !animController.IsInTransition(0))
        {
            rb.velocity = animController.deltaPosition * vso.rootMotionAtkSpeed / Time.deltaTime;
        }

        // Attacking root motion
        if (isAttacking && !disableDashing && !animController.IsInTransition(0))
        {
            float y = rb.velocity.y;

            rb.velocity = animController.deltaPosition * vso.rootMotionAtkSpeed / Time.deltaTime;

            rb.velocity = new Vector3(rb.velocity.x, y, rb.velocity.z);
        }

        // Jump Roll root motion
        if (isLandRolling && animController.GetBool("Grounded") && !isLanding)
        {
            isLanding = true;
            disableMovement = true;
            disableInputRotations = true;
            tallCollider.material = null;
        }
        if (isLanding)
        {
            AnimatorStateInfo jumpRollState = animController.GetCurrentAnimatorStateInfo(0);

            if (jumpRollState.IsName("JumpRoll") && jumpRollState.normalizedTime < 0.3f || animController.GetBool("Grounded") && animController.IsInTransition(0))
            {
                float y = rb.velocity.y;

                rb.velocity = animController.deltaPosition * vso.rootMotionJumpRollSpeed / Time.deltaTime;

                rb.velocity = new Vector3(rb.velocity.x, y, rb.velocity.z);

            }
            else if (isGrounded)
            {
                isLanding = false;
                disableMovement = false;
                disableJumping = false;
                disableInputRotations = false;
                tallCollider.material = friction;
                isLandRolling = false;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.identity;

        // Spawn player position
        if (vso.distanceSpawn >= 0 && vso.distanceSpawn <= pathCreator.path.length)
        {
            Vector3 spawnPosition = pathCreator.path.GetPointAtDistance(vso.distanceSpawn);
            spawnPosition.y += vso.spawnYOffset;

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(spawnPosition, 0.5f);
        }

        // Player ground check
        Vector3 point = new Vector3(transform.position.x + vso.groundCheckOffset.x, transform.position.y + vso.groundCheckOffset.y, transform.position.z + vso.groundCheckOffset.z) + Vector3.down;
        Gizmos.matrix = Matrix4x4.TRS(point, transform.rotation, transform.lossyScale);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, vso.groundCheckSize);

        // Player head check
        Vector3 centerPos = new Vector3(transform.position.x + vso.sneakCheckOffset.x, transform.position.y + vso.sneakCheckOffset.y, transform.position.z + vso.sneakCheckOffset.z) + Vector3.up;
        Gizmos.matrix = Matrix4x4.TRS(centerPos, transform.rotation, transform.lossyScale);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, vso.sneakCheckSize);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hitbox"))
        {
            EnemyNavigation enemyNav = other.GetComponentInParent<EnemyNavigation>();
            Vector3 enemyPos = enemyNav.transform.position;

            // Get dir from AI to player
            Vector3 facingDir = (other.ClosestPointOnBounds(transform.position) - enemyPos).IgnoreYAxis();
            Vector3 dir = enemyNav.CalculatePathFacingDir(enemyPos, facingDir);

            //TakeDamage(dir);
        }
    }

    #endregion

    #region Jump

    void JumpCooldownTimer()
    {
        if (jumpCounter > 0f)
        {
            animController.SetBool("Jump", false);

            jumpCounter -= Time.deltaTime;
        }
    }

    void CoytoteTime()
    {
        // Coyote Time
        if (isGrounded)
        {
            canDoubleJump = true;

            jumpCoyoteCounter = vso.jumpCoyoteTime;
        }
        else
        {
            jumpCoyoteCounter -= Time.deltaTime;
        }
    }

    #endregion

    void DashCooldown()
    {
        if (currentDashCooldown > 0f)
        {
            currentDashCooldown -= Time.deltaTime;
        }
    }

    void GroundCheck()
    {
        Vector3 centerPos = new Vector3(transform.position.x + vso.groundCheckOffset.x, transform.position.y + vso.groundCheckOffset.y, transform.position.z + vso.groundCheckOffset.z) + Vector3.down;

        bool overlap = Physics.CheckBox(centerPos, vso.groundCheckSize / 2, transform.rotation, ~vso.ignorePlayerMask);

        RaycastHit hit;
        if (Physics.Raycast(centerPos, Vector3.down, out hit, 100f, ~vso.ignorePlayerMask))
        {
            newGroundY = hit.point.y;
        }

        if (overlap)
        {
            isGrounded = true;
            animController.SetBool("Grounded", true);

            if (rb.velocity.y <= 1f)
                animController.SetBool("Fall", false);
        }
        else
        {
            isGrounded = false;
            animController.SetBool("Grounded", false);
        }
    }

    public void AdjustPlayerOnPath()
    {
        Vector3 pathPos = pathCreator.path.GetPointAtDistance(distanceOnPath, EndOfPathInstruction.Stop);
        //Debug.DrawLine(pathPos + new Vector3(0f, 1f, 0f), pathPos + Vector3.up * 3f, Color.green);

        // Distance between path and player
        float distance = Vector3.Distance(pathPos.IgnoreYAxis(), transform.position.IgnoreYAxis());

        // Direction from path towards player
        Vector3 dirTowardPlayer = transform.position.IgnoreYAxis() - pathPos.IgnoreYAxis();
        Debug.DrawLine(pathPos, pathPos + dirTowardPlayer * vso.maxDistancePath, Color.blue);

        // Keeps player on the path
        if (distance > vso.maxDistancePath)
        {
            Vector3 dirTowardPath = (pathPos.IgnoreYAxis() - transform.position.IgnoreYAxis()).normalized;
            rb.AddForce(dirTowardPath * vso.adjustVelocity, ForceMode.Impulse);
        }
    }

    void StoreInputMovement()
    {
        // Store when player presses left or right
        if (prevInputDirection != input.GetMovementInput.normalized)
        {
            // Reset speed when turning around
            currentSpeed = 2f;
            prevInputDirection = input.GetMovementInput.normalized;
        }
    }

    #region Animation Jog Speed

    void DetectAnimAcceleration(Vector3 targetVelocity, Vector3 direction)
    {
        #region Detect animation player input
        if (direction.magnitude > 0.1f)
        {
            if (!disableMovement)
                animController.SetBool("isMoving", true);
        }
        else
        {
            if (animIsDashing || animController.GetBool("isSneaking"))
            {
                animController.SetBool("isMoving", false);
            }
            animController.SetBool("isSprinting", false);
        }
        #endregion

    }

    

    #endregion

    #region Rotation

    void HandleRotation()
    {
        // Calculate player 2D rotation
        distanceOnPath = pathCreator.path.GetClosestDistanceAlongPath(transform.position);

        targetRot2D = Rotation2D(GetPathRotation(), input.GetMovementInput.normalized);
    }

    // Determine to update current or last input
    Quaternion Rotation2D(Quaternion targetRot2D, Vector3 direction)
    {
        if (disableUpdateRotations)
            return targetRot2D;

        // Flipping direction
        if (prevInputDirection.x < 0f)
        {
            Vector3 rot = targetRot2D.eulerAngles;
            targetRot2D = Quaternion.Euler(rot.x, rot.y + 180f, rot.z);
        }

        // Don't allow new inputs, but allow last input to update rotation
        if (disableInputRotations)
            UpdateRotation(previousRotation);
        else
            UpdateRotation(targetRot2D);


        if (input.isMovementHeld)
        {
            if (previousRotation != targetRot2D)
            {
                dampedTargetRotationPassedTime = 0f;
            }

            if (disableInputRotations)
                return targetRot2D;

            // Saved for deceleration
            previousRotation = targetRot2D;
            prevInputDirection = direction;
        }

        return targetRot2D;
    }

    void UpdateRotation(Quaternion targetRot2D)
    {
        

        float currentYAngle = rb.rotation.eulerAngles.y;
        if (currentYAngle == previousRotation.eulerAngles.y)
        {
            return;
        }

        float smoothedYAngle = Mathf.SmoothDampAngle(currentYAngle, targetRot2D.eulerAngles.y, ref dampedTargetRotationCurrentYVelocity, vso.timeToReachTargetRotation - dampedTargetRotationPassedTime);
        dampedTargetRotationPassedTime += Time.deltaTime;

        Quaternion targetRotation = Quaternion.Euler(0f, smoothedYAngle, 0f);
        rb.MoveRotation(targetRotation);
    }

    void CameraRotation()
    {
        // Rotate camera 2d
        Vector3 camEulerAngle = mainCamera.rotation.eulerAngles;
        virtualCam2D.transform.rotation = Quaternion.Slerp(mainCamera.rotation, Quaternion.Euler(camEulerAngle.x, GetPathRotation().eulerAngles.y - 90f, camEulerAngle.z), vso.camRotationSpeed2D);
    }

    #endregion


    public Quaternion GetPathRotation()
    {
        return pathCreator.path.GetRotationAtDistance(distanceOnPath, EndOfPathInstruction.Stop);
    }

}
