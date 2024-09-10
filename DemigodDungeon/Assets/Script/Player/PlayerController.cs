using System;
using System.Collections;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;


[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private enum MovementState
    {
        Grounded,
        Climbing,
        WallSliding,
        WallSticking,
        Grappling
    }

    [Header("Movement Parameters")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField, Range(0, 10)] private float dragCoefficient = 6f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.2f;

    [Header("Climb Parameters")]
    [SerializeField] private float climbJumpTime = 0.1f;

    [Header("Wall Jump Parameters")]
    [SerializeField] private float wallSlideTransitionTime = 0.2f;
    [SerializeField] private float wallJumpTime = 0.25f;
    [SerializeField] private Vector2 wallJumpForce;
    [SerializeField] private float wallSlideGravityMultiplier = 0.5f;
    [SerializeField] private float wallJumpBufferTime = 0.05f;
    [SerializeField] private float wallJumpCheckDistance = 0.2f;
    [SerializeField] private float wallJumpDelay = 0.2f;

    [Header("Grapple Parameters")]
    [SerializeField] private float grappleSpeed = 5f;
    [SerializeField] private float grappleDistance = 10f;
    [SerializeField] private float grappleCooldownTime = 1f;
    [SerializeField] private float grappleDelayTime = 0.1f;
    [SerializeField] private float grappleDrawTime = 0.1f;

    [SerializeField] private float grappleMoveTime = 1f;
    [SerializeField] private float grappleMoveSpeed = 5f;
    
    [SerializeField] private float waveDashBufferTime = 0.2f;
    [SerializeField] private float waveDashReturnTime = 0.4f;
    [SerializeField] private float waveDashReturnSpeed = 10f;

    private float grappleCDTimer;
    private int grappleFrameCount;
    [SerializeField] private LineRenderer grappleLine;
    [SerializeField] private GameObject webModelPrefab;
    private GameObject webModel;
    public bool grappling, grappleTargetFound, beginGrappleMove, grappleVelUp;
    private Vector3 grapplePosition, grapplePlayerOriginalPos;
    private GameObject grappleTarget;
    private float waveDashMult = 1;

    private bool beginWaveDash, overrideHorizontalMovement;

    private ParticleSystem grappleParticles;
    [SerializeField] private ParticleSystem grappleCircleParticle;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask ropeLayer;
    [SerializeField] private LayerMask webWallLayer;

    private LayerMask unGrappleLayer => groundLayer | webWallLayer;

    [Header("Animation Parameters")]
    [SerializeField] private GameObject playerModel;

    [SerializeField] private Animator anim;

    private Vector3 modelPos;
    private Vector3 targetRotation;
    private float lastNonZeroVertical;

    [Header("FMOD Sounds")]
    [SerializeField] private EventReference jumpSoundRef;
    [SerializeField] private EventReference fallingSoundRef;
    [SerializeField] private EventReference landSoundRef;
    [SerializeField] private EventReference slideSoundRef;
    [SerializeField] private EventReference walkSoundRef;
    [SerializeField] private EventReference pickupSoundRef;
    [SerializeField] private EventReference webShootSoundRef;
    [SerializeField] private EventReference webHitSoundRef;
    [SerializeField] private EventReference dashSoundRef;
    [SerializeField] private float walkSoundRate = 10f;

    private CharacterController controller;
    private CapsuleCollider capCollider;

    private Vector3 moveDirection;

    private Vector3 halfExtent;

    private Coroutine climbJumpCoroutine, wallJumpCoroutine, grapplingCoroutine, waveDashReturnCoroutine;
    private bool? isLeftJump = null;

    private MovementState movementState = MovementState.Grounded;
    private MovementState lastState = MovementState.Grounded;
    [Tooltip("Raycast collision object returned in CheckOnWall()")]
    private GameObject wallLeft, wallRight;

    private float jumpCoyoteTimer, jumpBufferTimer, wallJumpBufferTimer, wallJumpDelayTimer, waveDashReturnTimer;
    private float jumpHeight;
    private bool lastGround = true;

    private float wallSlideTransitionTimer;

    private bool isJumping, isWalking, freezeMovement;

    private EventInstance jumpSound;
    private EventInstance fallingSound;
    private EventInstance landSound;
    private EventInstance slideSound;
    private EventInstance pickupSound;
    private EventInstance walkSound;
    private EventInstance webShootSound;
    private EventInstance webHitSound;
    private EventInstance dashSound;
    private Coroutine walkSoundRoutine;

    private void Awake()
    {
        jumpSound = RuntimeManager.CreateInstance(jumpSoundRef);
        fallingSound = RuntimeManager.CreateInstance(fallingSoundRef);
        landSound = RuntimeManager.CreateInstance(landSoundRef);
        slideSound = RuntimeManager.CreateInstance(slideSoundRef);
        pickupSound = RuntimeManager.CreateInstance(pickupSoundRef);
        walkSound = RuntimeManager.CreateInstance(walkSoundRef);
        webShootSound = RuntimeManager.CreateInstance(webShootSoundRef);
        webHitSound = RuntimeManager.CreateInstance(webHitSoundRef);
    }

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        capCollider = GetComponent<CapsuleCollider>();
        grappleLine = GetComponent<LineRenderer>();
        grappleParticles = GetComponent<ParticleSystem>();
        
        grappleParticles.Stop();
        grappleCircleParticle.Stop();
        grappleCircleParticle.Clear();
        
        StopGrapple();

        modelPos = playerModel.transform.localPosition;
        walkSoundRoutine = StartCoroutine(WalkSoundLoop());
    }
    private void OnEnable()
    {
        walkSoundRoutine = StartCoroutine(WalkSoundLoop());
    }

    // Update is called once per frame
    void Update()
    {
        if (PauseManager.Instance.isPaused) return;
        
        if (LevelManager.Instance != null && LevelManager.Instance.isRespawning)
        {
            FreezeMovement();
            playerModel.transform.rotation = Quaternion.Euler(0, 0, 0);
            movementState = MovementState.Grounded;
            wallJumpCoroutine = null;
            climbJumpCoroutine = null;
            StopAllCoroutines();
            StopGrapple();
            return;
        }


        ProcessCoyoteTimers();

        ProcessMovement();

        CheckOnWall();

        CheckState();

        switch (movementState)
        {
            case MovementState.Grounded:
                ProcessJump();
                break;
            case MovementState.Climbing:
                ProcessClimb();
                break;
            case MovementState.WallSliding:
                ProcessWallJump();
                break;
            case MovementState.WallSticking:
                ProcessWallStick();
                ProcessWallJump();
                break;
            case MovementState.Grappling:
                ExecuteGrappleMove();
                break;
        }

        ProcessGrapple();

        RotatePlayerModel();

        ApplyFinalMovements();
    }

    private void LateUpdate()
    {
        ResetGrappleStart();
    }


    private void ProcessCoyoteTimers()
    {
        if (controller.isGrounded)
        {
            jumpCoyoteTimer = coyoteTime;
        }
        else
        {
            jumpCoyoteTimer -= Time.deltaTime;
        }

        if (InputManager.Instance.Jump.WasPressedThisFrame())
        {
            jumpBufferTimer = jumpBufferTime;
            wallJumpBufferTimer = wallJumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
            wallJumpBufferTimer -= Time.deltaTime;
        }

        if (wallRight != null || wallLeft != null)
        {
            wallJumpDelayTimer += Time.deltaTime;
        }
        else
        {
            wallJumpDelayTimer = 0;
        }
    }

    private void CheckState()
    {
        Vector3 pos = transform.position;
        lastState = movementState;

        halfExtent = new Vector3(capCollider.radius, capCollider.height / 2, capCollider.radius);

        if (beginGrappleMove)
        {
            TransitionState(MovementState.Grappling);
        }
        else if (Physics.CheckBox(pos, halfExtent, Quaternion.identity, ropeLayer))
        {
            if (InputManager.Instance.Move.ReadValue<Vector2>().y > 0)
            {
                TransitionState(MovementState.Climbing);
                moveDirection.x = 0;
            }
        }
        else if (Physics.CheckBox(pos, halfExtent, Quaternion.identity, webWallLayer))
        {
            GameObject webBlock = Physics.OverlapBox(pos, halfExtent, Quaternion.identity, webWallLayer)[0].gameObject;

            if (webBlock.transform.rotation.eulerAngles.z == 0)
            {
                if (InputManager.Instance.Move.ReadValue<Vector2>().x > 0)
                {
                    TransitionState(MovementState.WallSticking);
                    moveDirection.x = 0;
                }
            }
            else if (webBlock.transform.rotation.eulerAngles.z == 180)
            {
                if (InputManager.Instance.Move.ReadValue<Vector2>().x < 0)
                {
                    TransitionState(MovementState.WallSticking);
                    moveDirection.x = 0;
                }
            }
        }
        else if ((wallLeft != null || wallRight != null) && !controller.isGrounded)
        {
            if (wallLeft != null && InputManager.Instance.Move.ReadValue<Vector2>().x < 0)
            {
                if (wallSlideTransitionTimer > wallSlideTransitionTime)
                    TransitionState(MovementState.WallSliding);
                else if (!Physics.CheckBox(pos, new Vector3(0.1f, capCollider.height / 2 + 0.2f, 0.1f), Quaternion.identity, groundLayer))
                    wallSlideTransitionTimer += Time.deltaTime;
                else
                    wallSlideTransitionTimer = 0;
            }
            else if (wallRight != null && InputManager.Instance.Move.ReadValue<Vector2>().x > 0)
            {
                if (wallSlideTransitionTimer > wallSlideTransitionTime)
                    TransitionState(MovementState.WallSliding);
                else if (!Physics.CheckBox(pos, new Vector3(0.1f, capCollider.height / 2 + 0.2f, 0.1f), Quaternion.identity, groundLayer))
                    wallSlideTransitionTimer += Time.deltaTime;
                else
                    wallSlideTransitionTimer = 0;
            }
        }

        else
        {
            if (wallJumpCoroutine == null)
                TransitionState(MovementState.Grounded);
            else// if (!controller.isGrounded)
            {
                if (Physics.CheckBox(pos, halfExtent, Quaternion.identity, webWallLayer))
                {
                    TransitionState(MovementState.WallSticking);
                }
                else if (Physics.CheckBox(pos, halfExtent, Quaternion.identity, groundLayer))
                {
                    TransitionState(MovementState.WallSliding);
                }
            }
        }

        if (lastGround == true && controller.isGrounded == false)
        {
        }
        if (lastGround == false && controller.isGrounded == true)
        {
            playLandSound();
        }
        lastGround = controller.isGrounded;

        float y = transform.position.y;
        if (controller.isGrounded)
        {
            jumpHeight = y;
        }
        else if (y > jumpHeight)
        {
            jumpHeight = y;
        }
        Vector3 velocity = gameObject.GetComponent<Rigidbody>().velocity;
        if (!controller.isGrounded && velocity.y < 0)
        {
            fallingSound.setParameterByName("Intensity", clampMap(velocity.magnitude, 0, 20, 0, 1));
        }
    }
    private void TransitionState(MovementState state)
    {
        movementState = state;
        if (lastState != state)
        {
            //These happen when the player just switched to that state
            switch (state)
            {
                case MovementState.Grounded:
                    wallSlideTransitionTimer = 0;
                    slideSound.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    break;
                case MovementState.Climbing:
                    isJumping = false;
                    break;
                case MovementState.WallSliding:
                    slideSound.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    slideSound.start();
                    moveDirection.y *= 0.5f;
                    isJumping = false;
                    break;
                case MovementState.WallSticking:
                    slideSound.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    isJumping = false;
                    break;
            }
        }
    }

    #region WallJump

    private void CheckOnWall()
    {
        float activeDistance = capCollider.radius + wallJumpCheckDistance;

        if (Physics.Raycast(transform.position, Vector3.right, out RaycastHit hit, activeDistance, groundLayer))
        {
            wallRight = hit.collider.gameObject;
        }
        else
        {
            wallRight = null;
        }

        if (Physics.Raycast(transform.position, Vector3.left, out hit, activeDistance, groundLayer))
        {
            wallLeft = hit.collider.gameObject;
        }
        else
        {
            wallLeft = null;
        }


    }

    private void ProcessWallJump()
    {

        if (wallJumpBufferTimer > 0 && wallJumpDelayTimer > wallJumpDelay)
        {
            if (movementState == MovementState.WallSliding)
            {
                jumpSound.setParameterByName("Type", 0); //rock
            }
            else if (movementState == MovementState.WallSticking)
            {
                jumpSound.setParameterByName("Type", 1); //web
            }
            jumpSound.start();
            slideSound.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

            bool groundBelow = Physics.CheckBox(transform.position,
                new Vector3(0.1f, capCollider.height / 2 + 0.2f, 0.1f),
                Quaternion.identity, groundLayer);


            if (wallRight != null && !groundBelow)
            {
                wallJumpCoroutine ??= StartCoroutine(WallJump(true));
            }
            else if (wallLeft != null && !groundBelow)
            {
                wallJumpCoroutine ??= StartCoroutine(WallJump(false));
            }

            wallJumpBufferTimer = 0;
        }

        moveDirection.y -= gravity * (wallJumpCoroutine == null ? wallSlideGravityMultiplier : 1) * Time.deltaTime;
    }

    private IEnumerator WallJump(bool isRight)
    {
        isLeftJump = !isRight;

        isJumping = true;

        float startTime = Time.time;

        moveDirection.y = wallJumpForce.y;
        moveDirection.x = isRight ? -wallJumpForce.x : wallJumpForce.x;

        while (Time.time < startTime + wallJumpTime)
        {
            yield return null;
        }

        isLeftJump = null;
        wallJumpCoroutine = null;
    }

    #endregion



    private void ProcessMovement()
    {
        float horizontal = InputManager.Instance.Move.ReadValue<Vector2>().x;
        float curY = moveDirection.y;
        float curX = moveDirection.x;

        if (wallJumpCoroutine == null)
        {
            if (horizontal != 0)
            {
                if (moveDirection.x > 1f && horizontal < 0)
                    moveDirection += new Vector3(horizontal, 0, 0);
                else if (moveDirection.x < -1f && horizontal > 0)
                    moveDirection += new Vector3(horizontal, 0, 0);
                else
                    moveDirection = new Vector3(horizontal, curY, 0);
            }
            else
            {
                moveDirection = new Vector3(curX * (1f / 180f) * (50 + dragCoefficient), curY, 0);
                
                if (Mathf.Abs(curX) < 0.1f)
                {
                    moveDirection.x = 0;
                }
            }
        }
    }

    #region Climbing

    private void ProcessClimb()
    {
        float vertical = InputManager.Instance.Move.ReadValue<Vector2>().y;

        if (climbJumpCoroutine == null)
            moveDirection.y = vertical;

        if (jumpBufferTimer > 0)
        {
            climbJumpCoroutine ??= StartCoroutine(ClimbJump());

            jumpBufferTimer = 0;
        }
    }

    private IEnumerator ClimbJump()
    {
        float startTime = Time.time;


        while (Time.time < startTime + climbJumpTime)
        {
            moveDirection.y = jumpForce;

            yield return null;
        }

        climbJumpCoroutine = null;
    }

    #endregion

    private void ProcessWallStick()
    {
        float vertical = InputManager.Instance.Move.ReadValue<Vector2>().y;

        if (wallJumpCoroutine == null)
            moveDirection.y = vertical;
    }

    private void ProcessJump()
    {

        if (controller.velocity.y > 0)
        {
            isJumping = true;
        }
        else
        {
            isJumping = false;
        }

        if (jumpCoyoteTimer > 0)
        {
            if (jumpBufferTimer > 0)
            {
                moveDirection.y = jumpForce;

                jumpCoyoteTimer = 0;
                jumpBufferTimer = 0;

                jumpSound.setParameterByName("Type", 0); //rock
                jumpSound.start();
            }
        }
        else
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }
    }

    private void playLandSound()
    {
        float impact = clampMap(jumpHeight - transform.position.y, 2, 10, 0, 1);
        landSound.setParameterByName("Impact", impact);
        landSound.start();
    }

    private bool HittingCeiling()
    {
        Vector3 ceilingCheckCenter = transform.position + new Vector3(0, capCollider.height / 2 - 0.1f, 0);

        return Physics.CheckBox(ceilingCheckCenter, new Vector3(0.1f, 0.01f, 0.1f), Quaternion.identity, groundLayer);
    }

    private void ApplyFinalMovements()
    {
        AddGrapplingVelocity();
        
        if (HittingCeiling())
        {
            moveDirection.y = Math.Min(0, moveDirection.y);
        }

        controller.Move(moveDirection * moveSpeed * Time.deltaTime);
    }

    float clampMap(float a, float l1, float r1, float l2, float r2)
    {
        return Math.Clamp((a - l1) * (r2 - l2) / (r1 - l1) + l2, 0, 1);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeathTrigger"))
        {
            LevelManager.Instance.Respawn();
            CameraController.Instance.BeginScreenShake(0.5f, 10f);
        }

        if (other.CompareTag("Enemy"))
        {
            other.TryGetComponent(out Freezable freezable);
            if (!freezable.isFrozen)
            {
                LevelManager.Instance.Respawn();
                CameraController.Instance.BeginScreenShake(0.5f, 10f);
            }
        }

        if (other.CompareTag("RespawnPoint"))
        {
            LevelManager.Instance.SetRespawnPoint(other.transform.GetChild(0).transform.position);
        }

        if (other.CompareTag("Collectible"))
        {
            GameManager.Instance.AddCollectible(LevelManager.Instance.currentScene, other.gameObject);

            GameObject particle = Instantiate(other.GetComponent<CollectibleController>().collectParticle, other.transform.position, Quaternion.identity);
            Destroy(particle, 1.5f);

            pickupSound.start();
        }
        
        if (other.CompareTag("End"))
        {
            UIManager.Instance.EndGame();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("SceneCollider"))
        {
            LevelManager.Instance.currentScene = other.gameObject.scene.name;
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Crushable"))
        {
            Collider[] topCollisions = Physics.OverlapBox(transform.position + new Vector3(0, 0.3f, 0),
                new Vector3(0.1f, capCollider.height / 2 + 0.2f, 0.1f), Quaternion.identity, groundLayer);

            if (topCollisions.Length > 0)
            {
                Collider topCollision = topCollisions[0];
                if (topCollision.gameObject == other.gameObject &&
                    Physics.CheckBox(transform.position, new Vector3(0.1f, capCollider.height / 2 + 0.2f, 0.1f),
                        Quaternion.identity, groundLayer))
                {
                    LevelManager.Instance.Respawn();
                }
            }

        }
    } 

    private void RotatePlayerModel()
    {
        switch (movementState)
        {


            case MovementState.Grounded:

                isWalking = InputManager.Instance.Move.ReadValue<Vector2>().x != 0;

                lastNonZeroVertical = 0;

                float horizontal = InputManager.Instance.Move.ReadValue<Vector2>().x;

                if (isLeftJump == null)
                {
                    if (horizontal > 0)
                    {
                        targetRotation = new Vector3(0, 180, 0);
                    }
                    else if (horizontal < 0)
                    {
                        targetRotation = new Vector3(0, 0, 0);
                    }
                    else
                    {
                        targetRotation = new Vector3(0, playerModel.transform.rotation.eulerAngles.y, 0);
                    }
                }
                else
                {
                    if (isLeftJump == true)
                    {
                        targetRotation = new Vector3(0, 180, 0);
                    }
                    else
                    {
                        targetRotation = new Vector3(0, 0, 0);
                    }
                }

                break;
            case MovementState.Climbing:

                isWalking = InputManager.Instance.Move.ReadValue<Vector2>() != Vector2.zero;

                bool groundBelow = Physics.CheckBox(transform.position,
                    halfExtent + new Vector3(0, 0.2f, 0), Quaternion.identity, groundLayer);

                if (!groundBelow)
                {
                    targetRotation = new Vector3(0, 90, -90);
                }
                else
                {
                    targetRotation = new Vector3(0, 90, 0);
                }

                break;
            case MovementState.WallSliding:

                if (wallJumpCoroutine == null)
                {
                    targetRotation = new Vector3(180, wallLeft != null ? 180 : 0, 90);
                }
                else
                {
                    if (isLeftJump == true)
                    {
                        targetRotation = new Vector3(0, 180, 0);
                    }
                    else
                    {
                        targetRotation = new Vector3(0, 0, 0);
                    }
                }

                break;
            case MovementState.WallSticking:

                isWalking = InputManager.Instance.Move.ReadValue<Vector2>() != Vector2.zero;

                if (wallJumpCoroutine == null)
                {
                    float vertical = InputManager.Instance.Move.ReadValue<Vector2>().y;

                    if (vertical != 0)
                    {
                        lastNonZeroVertical = vertical;
                    }

                    float x = targetRotation.x;
                    float y = wallLeft != null ? 180 : 0;
                    float z = 90;

                    if (lastNonZeroVertical == 0)
                    {
                        x = 180;
                    }

                    if (vertical > 0)
                    {
                        x = 180;
                    }
                    else if (vertical < 0)
                    {
                        x = 0;
                    }

                    targetRotation = new Vector3(x, y, z);
                }
                else
                {
                    if (isLeftJump == true)
                    {
                        targetRotation = new Vector3(0, 180, 0);
                    }
                    else
                    {
                        targetRotation = new Vector3(0, 0, 0);
                    }
                }

                break;

            case MovementState.Grappling:
                targetRotation = Quaternion.LookRotation(grapplePosition - transform.position).eulerAngles;
                break;

        }
        playerModel.transform.localPosition = modelPos;

        playerModel.transform.rotation = Quaternion.Slerp(playerModel.transform.rotation, Quaternion.Euler(targetRotation), 0.1f);

        anim.SetBool("isWalking", isWalking);
        anim.SetBool("isJumping", isJumping);
    }

    public void FreezeMovement()
    {
        moveDirection = Vector3.zero;
    }

    #region Grapple

    private void ResetGrappleStart()
    {
        if (grappling)
        {
            grappleLine.SetPosition(0, transform.position);

        }


    }

    private void ProcessGrapple()
    {

        Vector2 grappleDirection = InputManager.Instance.Move.ReadValue<Vector2>().normalized;

        if (grappleDirection == Vector2.zero)
        {
            grappleDirection = -playerModel.transform.right;
            grappleDirection.Normalize();
        }

        if (!grappleTargetFound)
        {
            if (grappleCDTimer <= 0)
            {
                if (InputManager.Instance.Grapple.WasPressedThisFrame())
                {
                    StartGrapple(new Vector3(grappleDirection.x, grappleDirection.y, 0));
                }

            }
        }
        else
        {
            MaintainGrapple(grapplePosition - transform.position);

            if (grappleCDTimer <= 0)
            {
                if (InputManager.Instance.Grapple.WasPressedThisFrame())
                {
                    beginGrappleMove = true;
                    ExecuteGrappleMove();
                }
            }
        }

        if (grappleCDTimer > 0)
        {
            grappleCDTimer -= Time.deltaTime;
        }
    }

    private void MaintainGrapple(Vector3 grappleDirection)
    {

        if (Physics.Raycast(transform.position, grappleDirection, out RaycastHit hit, grappleDistance, unGrappleLayer))
        {
            if (Vector3.Distance(hit.point, grapplePosition) > 0.2f)
            {
                StopGrapple();
            }
        }
    }

    private void StartGrapple(Vector3 grappleDirection)
    {
        webShootSound.start();
        if (grappleCDTimer > 0) return;


        grappling = true;



        if (Physics.SphereCast(transform.position, 0.1f, grappleDirection, out RaycastHit hit, grappleDistance, webWallLayer))
        {
            grapplePosition = hit.point;
            grappleTarget = hit.collider.gameObject;

            if (grappleTargetFound == false) webHitSound.start();
            grappleTargetFound = true;

        }
        else
        {
            Physics.Raycast(transform.position, grappleDirection, out RaycastHit hit2, grappleDistance, unGrappleLayer);

            if (hit2.collider != null)
            {
                grapplePosition = hit2.point;
            }
            else
            {
                grapplePosition = transform.position + grappleDirection.normalized * grappleDistance;
            }

            grappleTargetFound = false;

            Invoke(nameof(StopGrapple), grappleDelayTime);
        }

        StartCoroutine(DrawGrappleLine(transform.position, grapplePosition));
    }


    IEnumerator DrawGrappleLine(Vector3 originPosition, Vector3 targetPosition)
    {
        grappleLine.enabled = true;
        float startTime = Time.time;

        while (Time.time < startTime + grappleDrawTime)
        {
            grappleLine.SetPosition(1, Vector3.Lerp(originPosition, targetPosition, (Time.time - startTime) / grappleDrawTime));

            yield return null;
        }
        
        grappleLine.SetPosition(1, targetPosition);


        if (webModel != null)
        {
            Destroy(webModel);
        }
        
        if (grappleTargetFound)
        {
            Quaternion webModelRotation = Quaternion.identity;

            if (grappleTarget.transform.rotation.eulerAngles.z == 0)
            {
                webModelRotation = Quaternion.Euler(0, 0, -90);
            }
            else if (grappleTarget.transform.rotation.eulerAngles.z >= 80 && grappleTarget.transform.rotation.eulerAngles.z <= 100)
            {
                webModelRotation = Quaternion.Euler(0, 0, 0);
            }
            else if (grappleTarget.transform.rotation.eulerAngles.z <= 190 && grappleTarget.transform.rotation.eulerAngles.z >= 170)
            {
                webModelRotation = Quaternion.Euler(0, 0, 90);
            }
            else
            {
                webModelRotation = Quaternion.Euler(0, 0, 180);
            }

            webModel = Instantiate(webModelPrefab, targetPosition, webModelRotation);
            webModel.transform.localScale *= 0.5f;
        }
        else
        {
            Quaternion webModelRotation = Quaternion.Euler(targetPosition - transform.position - new Vector3(0, 0, 90));
            webModel = Instantiate(webModelPrefab, targetPosition, webModelRotation);
            webModel.transform.localScale *= 0.5f;
        }

    }

    private void ExecuteGrappleMove()
    {
        grappleParticles.Play();
        grappleCircleParticle.Play();
        grappleCDTimer = grappleCooldownTime;

        grapplingCoroutine ??= StartCoroutine(GrappleMove());

        dashSound.start();
    }

    private IEnumerator GrappleMove()
    {
        grapplePlayerOriginalPos = transform.position;
        
        beginGrappleMove = true;

        float startTime = Time.time;
        
        float waveDashTimeStamp = 0;
        float waveDashTime = 0;
        bool stopChecking = false;
        
        while (Time.time < startTime + grappleMoveTime)
        {
            if (Vector3.Distance(transform.position, grapplePosition) < capCollider.height / 2 - 0.2f)
            {
                moveDirection.x = 0;
                StopGrapple();
                break;
            }
            
            if (InputManager.Instance.Jump.WasPressedThisFrame())
            {
                stopChecking = true;
            }
            
            if (!stopChecking) waveDashTimeStamp += Time.deltaTime;
            
            waveDashTime += Time.deltaTime;

            yield return null;
        }
        
        
        grapplingCoroutine = null;
        beginWaveDash = waveDashTime - waveDashTimeStamp < waveDashBufferTime;
        
        grappleVelUp = waveDashTime - grappleMoveTime < 0;
        print ("WaveDash: " + beginWaveDash + " " +  grappleVelUp);    

        
        
        moveDirection.y = 0;
        
        

        StopGrapple();
    }
    
    void AddGrapplingVelocity()
    {
        if (grapplingCoroutine != null)
        {
            moveDirection = (grapplePosition - grapplePlayerOriginalPos).normalized * grappleSpeed;
            moveDirection.y -= gravity * Time.deltaTime;
        }
        

        else if (grappleVelUp)
        {
            print("asf");
            if (beginWaveDash)
            {
                overrideHorizontalMovement = true;
                moveDirection.y = (grapplePosition - grapplePlayerOriginalPos).normalized.y * waveDashReturnSpeed; 
                moveDirection.y = Mathf.Abs(moveDirection.y);
                
                Invoke(nameof(ResetWaveDash), waveDashReturnTime * 0.01f);
            }
            
            if (overrideHorizontalMovement)
            {
                if (waveDashReturnTimer < waveDashReturnTime)
                {
                    waveDashReturnTimer += Time.deltaTime;
                    moveDirection.x = (grapplePosition - grapplePlayerOriginalPos).normalized.x * waveDashReturnSpeed *
                                      waveDashMult;
                    waveDashMult *= 0.99f;
                }
                else
                {
                    overrideHorizontalMovement = false;
                    waveDashReturnTimer = 0;
                    waveDashMult = 1;
                }
            }
            
            //waveDashReturnCoroutine ??= StartCoroutine(WaveDashReturn(0.4f));
        }
        
        //print (moveDirection);
    }
    
    void ResetWaveDash()
    {
        beginWaveDash = false;
    }
    
    IEnumerator WaveDashReturn(float time)
    {
        moveDirection.x = (grapplePosition - grapplePlayerOriginalPos).normalized.x * grappleSpeed;
        
        float startTime = Time.time;
        
        while (Time.time < startTime + time)
        {
            //moveDirection.x *= 0.9f;
            
            yield return null;
        }
        
        overrideHorizontalMovement = false;
        waveDashReturnCoroutine = null;
    }

    private void StopGrapple()
    {
        grappleLine.enabled = false;
        grappling = false;
        grappleTargetFound = false;
        beginGrappleMove = false;
        grapplingCoroutine = null;
        
        grappleParticles.Stop();
        grappleCircleParticle.Stop();
        grappleCircleParticle.Clear();

        if (webModel != null)
        {
            Destroy(webModel);
        }

        TransitionState(MovementState.Grounded);

        grappleCDTimer = grappleCooldownTime;
    }
    #endregion
    void playWalkSound()
    {
        if (movementState == MovementState.Grounded && controller.isGrounded && isWalking)
        {
            walkSound.setParameterByName("Type", 0); //regular ground
            walkSound.start();
        }
        else if (movementState == MovementState.Climbing || movementState == MovementState.WallSticking)
        {
            if (isWalking)
            {
                walkSound.setParameterByName("Type", 1); //web
                walkSound.start();
            }
        }
    }
    IEnumerator WalkSoundLoop()
    {
        print("coroutine started");
        while (true)
        {
            print("walk");
            playWalkSound();
            yield return new WaitForSeconds(1 / walkSoundRate);
        }
    }
}
