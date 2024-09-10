using System;
using System.Collections;

using System.Collections.Generic;

using UnityEngine;



public class PlayerManager : MonoBehaviour

{

    [Header("Basic Movement Parameters")]

    [Tooltip("Horizontal acceleration on the ground.")] public float groundMoveSpeed;

    [Tooltip("Horizontal acceleration in the air.")] public float airMoveSpeed;
    
    [Tooltip("Vertical acceleration on the ground.")] public float groundClimbSpeed;

    [Tooltip("Speed at which player slows down on the ground when no input is present.")] public float groundFriction;

    [Tooltip("Speed at which player slows down in the air when no input is present.")] public float airFriction;

    [Tooltip("Speed at which player velocity slows down on the ground if they are too fast.")] public float groundCorrectionSpeed;

    [Tooltip("Speed at which player velocity slows down in the air if they are too fast.")] public float airCorrectionSpeed;

    [Tooltip("Effect of gravity")] public float gravity;

    [Tooltip("Max horizontal speed on the ground.")] public float groundSpeedMax;

    [HideInInspector, Tooltip("Max horizontal speed in the air.")] public float airSpeedMax = 100;

    [Tooltip("Amount of force that is placed on player when they jump.")] public float jumpForce;

    [Tooltip("How much leneancy is there for an edge jump")] public float coyoteTime; // DO LATER AFTER INTERIM



    [HideInInspector] public bool IsGrounded { get; private set; }

    [HideInInspector] public float radius;

    [HideInInspector] public int groundLayer;

    [HideInInspector] private CapsuleCollider capCollider;

    [HideInInspector] public Vector2 displacement;

    [HideInInspector] public Rigidbody rb;

    [HideInInspector] public bool canJump { get; private set; }
    
    [HideInInspector] public bool canClimb { get; private set; }
    
    [HideInInspector] public bool onWeb { get; private set; }



    private void Awake()

    {

        capCollider = this.gameObject.GetComponent<CapsuleCollider>();



        rb = this.gameObject.GetComponent<Rigidbody>();

        
        canClimb = false;
    }



    // Start is called before the first frame update

    void Start()

    {

        groundLayer = 1 << 6;



        radius = capCollider.radius * 0.9f;

        Vector3 pos = transform.position + Vector3.down * (capCollider.height / 2) + Vector3.up * (radius * 0.9f);

        IsGrounded = Physics.CheckSphere(pos, radius, groundLayer);
    }


    // Update is called once per frame

    void Update()

    {
        Vector3 pos = transform.position + Vector3.down * (capCollider.height / 2) + Vector3.up * (radius * 0.9f);

        IsGrounded = Physics.CheckSphere(pos, radius, groundLayer);
        //Debug.Log(IsGrounded);
        
        canClimb = Physics.CheckSphere(pos, radius, 1 << 7);
        
        onWeb = Physics.CheckSphere(pos, radius, 1 << 8);
        
        
        if (IsGrounded || canClimb || onWeb)
        {
            displacement.y = Mathf.Max(displacement.y, 0);
        }
        else
        {
            displacement.y -= Time.deltaTime * gravity;
        }

        Move();
        if (InputManager.Instance.Jump.WasPressedThisFrame())
        {
            Jump();
        }
        
        if (canClimb)
        {
            Climb();
        }
        
        if (onWeb)
        {
            WallStick();
        }
    }



    private void FixedUpdate()
    {
        rb.velocity = new Vector3(displacement.x , displacement.y, 0);
    }



    public void Jump()
    {
        if (!IsGrounded) return;

        displacement.y = jumpForce;
    }



    public void Move()
    {
        float horMovement = InputManager.Instance.Move.ReadValue<Vector2>().x;

        float moveSpeed = IsGrounded ? groundMoveSpeed : airMoveSpeed;

        displacement.x = horMovement * moveSpeed;
        
    }

    public void Climb()
    {
        Vector3 pos = transform.position + Vector3.down * (capCollider.height / 2) + Vector3.up * (radius * 0.9f);
        
        bool onGround = Physics.CheckSphere(pos, radius, groundLayer);
        
        float vertMovement = InputManager.Instance.Move.ReadValue<Vector2>().y;
        

        if (vertMovement > 0)
        {
            transform.position += new Vector3(0, vertMovement * groundClimbSpeed, 0);
        }
        else if (vertMovement < 0)
        {
            if (!onGround)
            {
                transform.position += new Vector3(0, vertMovement * groundClimbSpeed, 0);
            }
        }
        else if (InputManager.Instance.Jump.WasPressedThisFrame())
        {
            displacement.y = jumpForce;
        }
    }

    public void WallStick()
    {
        Vector3 pos = transform.position + Vector3.down * (capCollider.height / 2) + Vector3.up * (radius * 0.9f);
        
        bool onGround = Physics.CheckSphere(pos, radius, groundLayer);
        
        float vertMovement = InputManager.Instance.Move.ReadValue<Vector2>().y;
        

        if (vertMovement > 0)
        {
            transform.position += new Vector3(0, vertMovement * groundClimbSpeed, 0);
        }
        else if (vertMovement < 0)
        {
            if (!onGround)
            {
                transform.position += new Vector3(0, vertMovement * groundClimbSpeed, 0);
            }
        }
        else if (InputManager.Instance.Jump.WasPressedThisFrame())
        {
            displacement.y = jumpForce;
        }
    }



    private IEnumerator CoyoteTime()
    {
        yield return new WaitForSeconds(coyoteTime);

        canJump = false;
    }
    
}

