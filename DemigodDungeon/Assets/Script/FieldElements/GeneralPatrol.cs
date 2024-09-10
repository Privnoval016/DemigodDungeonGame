// I MADE THIS
// WILLIAM LYU MADE THIS
// BECAUSE I CAN ONLY DO ALGORITHMS
// AND probably nothing else
// BUT REGARDLESS
// I EXIST
// LET THIS BE A TOKEN OF PROOF

using System;
using System.Collections;

using System.Collections.Generic;

using UnityEngine;

using Unity.VisualScripting;



[RequireComponent(typeof(Rigidbody))]



[Serializable, Inspectable]

public class Stop
{

    public Vector3 position;
    public float pauseTime;
    public float attemptTime;

    public Stop(Vector3 pos, float pause = 0, float attempt = 100)
    {
        position = pos;
        pauseTime = pause;
        attemptTime = attempt;
    }
}



public class GeneralPatrol : Freezable
 
{
    public enum EnemyType
    {
        Flying, 
        Walking
    }
    
    private enum PathEndModes { Reverse, Continue, Teleport, SelfDestruct };

    private enum MovementFailMode { Backtrack, Continue, Teleport }

    public enum MovementStatus { Waiting, Acceleration, Drifting, Deceleration, Pause };


    public EnemyType enemyType;

    [Header("Path")] 
    [SerializeField] private bool useLocalPositions;
    [SerializeField, Tooltip("If the path interset a rigidbody, it will be stopped.")] private Stop[] stops = new Stop[1];

    [Header("Movement Parameters")]

    [SerializeField, Tooltip("Positive float that determines the speed at which the object follow the path.\nNegative value will cause the object to never reach the stop it wants to go.")] private float speed = 1;
    [SerializeField, Tooltip("0-indexed value for which stop the object will start at (If the object is not there on awake, it will teleport there)")] private int startIndex;
    [SerializeField, Tooltip("If the path from this stop to the next stop is taking too long (1000 seconds by default):\nBacktrack will try to lead the object to the previous stop and retry.\nContinue will lead the object to the next stop.\nTeleport will set the position of the object to the destination stop.")] private MovementFailMode movementFailMode;
    [SerializeField, Tooltip("Whether the object should correct its position by force when arriving at the stop.")] private bool autoCorrectPos = false;
    [SerializeField, Tooltip("Whether the object should correct its velocity by force when arriving at the stop. (This option is useless if deceleration is -1)")] private bool autoCorrectVel = false;
    [SerializeField, Tooltip("How close the object has to be to each stop for it to be considered to be there")] private float accuracy = 1;
    [SerializeField, Tooltip("How accurate the direction of the enemy should be going in.")] private int directionalAccuracy = 10;
    [Header("End Behavior")]

    [SerializeField, Tooltip("Turning this off will destroy this component after the path is done.")] private bool loop = true;
    [SerializeField, Tooltip("What happens when object reaches the end of the defined path:\nReverse will cause the object to follow the path it took backwards.\nContinue will cause the object to goto the first stop again.\nTeleport will teleport the object to the first stop.\nSelfDestruct will destroy the GameObject the script is attached to once the path is done.")] private PathEndModes pathEndMode;
    [SerializeField, Tooltip("Whether the object pause after returning to the first stop (only work if PathEndMode is Teleport)")] private bool stopAfterEnd = false;

    [Header("Physics Parameters (Experimental)")]
    [SerializeField, Tooltip("Change this value to true if you want to use custom acceleration/deceleration.")] private bool useCustomParam = false;
    [SerializeField, Tooltip("Maximum accleration from standing still.")] private float acceleration = -1;
    [SerializeField, Tooltip("Maximum deceleration when moving.")] private float deceleration = -1;

    [Header("Debug")]
    [SerializeField, Tooltip("Whether system should give warning on invalid calls of functions.")] private bool overrideWarning = false;
    
    [Header("Animation")]
    [SerializeField] private GameObject model;

    private Animator animator;
    private Vector3 originalRotation;
    private Vector3 targetRotation;

    [HideInInspector] public int destinationIndex;
    [HideInInspector] public bool isAtStop;
    [HideInInspector] public Vector3 currentDirection;
    [HideInInspector] public MovementStatus currentStatus;
    [HideInInspector] public float distBeforeDecel = 0;
    [HideInInspector] public MovementStatus stateAfterWait;
    [HideInInspector] public Coroutine attemptTimer;
    [HideInInspector] public bool isPaused = false;
    [HideInInspector] public Vector3 heldVelocity = Vector3.zero;
    [HideInInspector] public MovementStatus heldStatus;

    private Rigidbody rb;

    // Start is called before the first frame update

    void Start()
    {
        rb = this.GetComponent<Rigidbody>();
        rb.useGravity = false;
        
        GetComponent<CapsuleCollider>().isTrigger = true;

        this.transform.position = stops[startIndex].position;
        destinationIndex = startIndex + 1;

        stateAfterWait = !useCustomParam ? MovementStatus.Drifting : MovementStatus.Acceleration;

        currentStatus = stateAfterWait;
        attemptTimer = StartCoroutine(WaitForEndOfAttempt(stops[destinationIndex].attemptTime));

        if (model != null)
        {
            if (model.GetComponent<Animator>() != null)
            {
                animator = model.GetComponent<Animator>();
            }
            else
            {
                animator = model.GetComponentInChildren<Animator>();
            }
        }

    }



    // Update is called once per frame

    void Update()
    {
        currentDirection = (stops[destinationIndex].position - transform.position).normalized;
        
        if (model != null && !isFrozen)
        { 
            if (rb.velocity.x > 0)
            {
                targetRotation = new Vector3(0, 90, 0);
            }
            else
            {
                targetRotation = new Vector3(0, -90, 0);
            }
            
            model.transform.rotation = Quaternion.Slerp(model.transform.rotation, Quaternion.Euler(targetRotation), 0.1f);
        }
        
        //print(currentStatus.ToString());

        // Update (or not) velocity depending of currentState
        switch (currentStatus)
        {
            case MovementStatus.Waiting:
                break;


            case MovementStatus.Acceleration:
                if (rb.velocity.magnitude < speed)
                {
                    rb.velocity += Time.deltaTime * acceleration * currentDirection;
                }
                if (useCustomParam) StartDecelerationCheck();
                break;


            case MovementStatus.Drifting:
                rb.velocity = currentDirection * speed;

                if (useCustomParam) StartDecelerationCheck();
                else if ((transform.position - stops[destinationIndex].position).magnitude < accuracy) // if distance between transform.position and destination position is very close (defined by accuracy)
                {
                    // arrive at stop check
                    if (autoCorrectPos) transform.position = stops[destinationIndex].position;
                    if (autoCorrectVel || (!useCustomParam)) rb.velocity = Vector3.zero;

                    StartCoroutine(WaitForChangeState(stops[destinationIndex].pauseTime, stateAfterWait));
                }
                break;


            case MovementStatus.Deceleration:
                rb.velocity += Time.deltaTime * deceleration * -currentDirection;

                if ((currentDirection + rb.velocity).magnitude < currentDirection.magnitude) // if started to accelerate in opposite direction
                {
                    // undershot
                    Debug.LogError("Lookie here do some debugging cuz this is not right");
                }

                if ((transform.position - stops[destinationIndex].position).magnitude < accuracy) // if distance between transform.position and destination position is very close (defined by accuracy)
                {
                    // arrive at stop check
                    if (autoCorrectPos) transform.position = stops[destinationIndex].position;
                    if (autoCorrectVel) rb.velocity = Vector3.zero;

                    StartCoroutine(WaitForChangeState(stops[destinationIndex].pauseTime, stateAfterWait));
                }
                break;


            case MovementStatus.Pause:
                break;


            default:
                Debug.LogError("currentStatus of " + this.gameObject.name.ToString() + " not valid, changed to Waiting");
                currentStatus = MovementStatus.Waiting;
                break;
        }

        // Cap speed
        if (rb.velocity.magnitude > speed)
        {
            rb.velocity = rb.velocity.normalized * speed;
        }
        
        if (isFrozen)
        {
            rb.velocity = Vector3.zero;
        }

        //set position of gazing
        lockPosition = transform.position;
    }



    private IEnumerator WaitForChangeState(float time, MovementStatus newState)
    {
        currentStatus = MovementStatus.Waiting;
        StopCoroutine(attemptTimer);

        while (time > 0)
        {
            while (isPaused)
            {
                yield return null;
            }
            yield return new WaitForSeconds(Time.deltaTime);
            time -= Time.deltaTime;
        }

        if (destinationIndex + 1 != stops.Length) destinationIndex++;
        else if (loop)
        {
            switch (pathEndMode)
            {
                case PathEndModes.Reverse:
                    Array.Reverse(stops);
                    destinationIndex = 1;
                    break;

                case PathEndModes.Continue:
                    destinationIndex = 0;
                    break;

                case PathEndModes.Teleport:
                    destinationIndex = stopAfterEnd ? 0 : 1;
                    transform.position = stops[0].position;
                    break;

                case PathEndModes.SelfDestruct:
                    Destroy(this.gameObject);
                    break;
            }
        }
        else
        {
            Destroy(this);
        }

        currentStatus = newState;
        attemptTimer = StartCoroutine(WaitForEndOfAttempt(stops[destinationIndex].attemptTime));
    }

    private IEnumerator WaitForEndOfAttempt(float time)
    {
        while (time > 0)
        {
            while (isPaused)
            {
                yield return null;
            }
            yield return new WaitForSeconds(Time.deltaTime);
            time -= Time.deltaTime ;
        }

        switch (movementFailMode)
        {
            case MovementFailMode.Backtrack:
                destinationIndex--;
                break;

            case MovementFailMode.Continue:
                destinationIndex++;
                break;

            case MovementFailMode.Teleport:
                transform.position = stops[destinationIndex].position;
                rb.velocity = Vector3.zero;
                break;
        }
    }



    private void StartDecelerationCheck()
    {
        if (!useCustomParam) return; // Use of this function assumes the use of custom parameters

        if (rb.velocity.magnitude * rb.velocity.magnitude / (2 * deceleration) <= GetDistToDestination())
        {
            currentStatus = MovementStatus.Deceleration;
        }
    }



    private float GetDistToDestination()
    {
        print("getting dist");
        return (stops[destinationIndex].position - this.transform.position).magnitude;
    }



    public void Pause()
    {
        if (isPaused)
        {
            if (!overrideWarning) Debug.LogWarning("Attempting to pause when object is already paused!", transform);
            return;
        }
        
        petrification.Petrify();
        
        if (animator != null) animator.speed = 0;
        
        isPaused = true;
        heldVelocity = rb.velocity;
        heldStatus = currentStatus;
        currentStatus = MovementStatus.Pause;
        rb.velocity = Vector3.zero;
        return;
    }



    public void Resume()
    {
        if (!isPaused)
        {
            if (!overrideWarning) Debug.LogWarning("Attempting to resume when object is not paused!", transform);
            return;
        }
        
        petrification.UnPetrify();
        
        if (animator != null) animator.speed = 1;
        isPaused = false;
        rb.velocity = heldVelocity;
        currentStatus = heldStatus;
        heldVelocity = Vector3.zero;
        heldStatus = MovementStatus.Pause;
        return;
    }
    
    protected override void ActivateFreeze()
    { 
        if (!isPaused)
        {
            GetComponent<CapsuleCollider>().isTrigger = false;
            Pause();
        }
    }
    
    protected override void ActivateUnfreeze()
    {
        if (isPaused)
        {
            GetComponent<CapsuleCollider>().isTrigger = true;
            Resume();
        }
    }

}

// LEST I FORGET MY BEING