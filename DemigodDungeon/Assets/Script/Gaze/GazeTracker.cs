using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tobii.Gaming;
using FMODUnity;
using FMOD.Studio;
using System.Linq;
using UnityEngine.Rendering;

public class GazeTracker : MonoBehaviour
{
    public static GazeTracker Instance { get; private set; }

    public bool usingEyeTracker = false;

    private GazePoint curGazePoint;
    [SerializeField] private Image gazeImage;
    [SerializeField] public Image[] gazeFillImages;
    [SerializeField] public float lerpRate = 20f;
    [SerializeField] public float maxFillAmount = 0.65f;
    [SerializeField] public float gazeRadius = 2f;
    [SerializeField] public float gazeResetTime = 1f;
    [SerializeField] public float gazeUnfreezeTime = 0.8f;
    [SerializeField] private float focusScale = 2f;
    [SerializeField] private float unfocusScale = 1.5f;
    [SerializeField] public float popScale;
    [SerializeField] private RectTransform gazeImageRect;
    [SerializeField] public GameObject[] rippleEffects;

    [SerializeField] private float gazeSlope = 0.5f;
    [SerializeField] private float particleToCircleRatio = 0.25f;

    [SerializeField] public GameObject petrifyEffect;
    [SerializeField] public GameObject unpetrifyEffect;

    private Camera cam;

    [Tooltip("The look location in world space")]
    private Vector2 lookPos;
    [Tooltip("cursor lerps towards this point (world space)")]
    private Vector2 targetGazeCursor;
    [Tooltip("The cursor position in world space")]
    private Vector3 gazeCursor;
    private Vector2 rectAnchoredPos;
    public GameObject gazeTarget;

    private float gazeTimer;

    public List<GameObject> frozenObjects;
    private List<float> unGazeTimers;

    private float targetScale;
    private float currentScale;

    [SerializeField] private EventReference freezeSoundRef;
    [SerializeField] private EventReference unfreezeSoundRef;

    private EventInstance freezeSound;
    private EventInstance unfreezeSound;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        frozenObjects = new List<GameObject>();
        unGazeTimers = new List<float>();

        Screen.SetResolution(1920, 1080, true);
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        cam = Camera.main;

        freezeSound = RuntimeManager.CreateInstance(freezeSoundRef);
        unfreezeSound = RuntimeManager.CreateInstance(unfreezeSoundRef);

        usingEyeTracker = SaveManager.Instance.gameData.usingEyeTracker;
    }
    

    // Update is called once per frame
    void Update()
    {
        if (LevelManager.Instance.movingToNextScene) return;

        UpdateLookPos();
        UpdateFreezeTarget();
        UpdatePreviousTarget();

        UpdateGazeGraphics();
    }

    void UpdateLookPos()
    {
        Vector2 mousePos = new Vector2(0,0);

        if (usingEyeTracker)
        {
            curGazePoint = TobiiAPI.GetGazePoint();
            if (curGazePoint.IsRecent())
            {
                /*  mousePos = curGazePoint.Screen;
                 mousePos.x -= Screen.width / 2f;
                 mousePos.y -= Screen.height / 2f;
                 mousePos *= gazeSlope * Screen.height + gazeYIntercept; */

                lookPos = ProjectToPlaneInWorld(curGazePoint, 31.5f);
            }

            //gazeImageRect.anchoredPosition = mousePos;
            //mousePos = gazeImageRect.transform.position;
        }
        else
        {
            mousePos = Input.mousePosition;
            lookPos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -cam.transform.position.z));
        }
       // lookPos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -cam.transform.position.z));
    }
    void UpdateFreezeTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(lookPos, gazeRadius);

        if (colliders.Length == 0)
        {
            gazeTarget = null;
            gazeTimer = 0f;
        }

        float lowestDist = 9999f;
        foreach (Collider c in colliders)
        {
            GameObject target = c.gameObject;
            Vector3 closestPoint = c.ClosestPoint(lookPos);
            if (target.TryGetComponent(out Freezable freezable)) //only freeze if object has freezable component
            {
                //disregard objects bigger than the lowestDist
                float dist = Vector3.Distance(closestPoint, lookPos);
                if (dist > lowestDist) break;
                /* else */
                lowestDist = dist;

                if (frozenObjects.Contains(target))
                {   //reset unfreeze cooldown on frozen objects
                    gazeTarget = target;
                    gazeTimer = gazeResetTime;

                    int index = frozenObjects.IndexOf(target);
                    unGazeTimers[index] = 0f; //reset unfreeze cooldown
                    break;
                }

                if (target != gazeTarget)
                {   //Start freezing new target
                    gazeTarget = target;
                    gazeTimer = 0f;

                    RuntimeManager.AttachInstanceToGameObject(freezeSound, transform);
                    freezeSound.start();
                    break;
                }

                if (gazeTimer <= gazeResetTime)
                {   //progress freezing timers
                    gazeTimer += Time.deltaTime;
                }
                else
                {   //object gets frozen in this tick
                    frozenObjects.Add(target);
                    unGazeTimers.Add(0f);

                    freezable.TriggerFreeze();
                    currentScale = popScale; //make the cursor pop
                }
            }
        }

        if (gazeTarget == null || !colliders.Contains(gazeTarget.GetComponent<Collider>()))
        {   //when target is no longer focused
            gazeTarget = null;
            targetGazeCursor = lookPos;
            gazeTimer = 0f;

            if (!frozenObjects.Contains(gameObject))
            {
                freezeSound.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }
        }
        else
        {
            targetGazeCursor = gazeTarget.GetComponent<Freezable>().lockPosition;
        }
        gazeCursor = Vector3.Lerp(gazeCursor, targetGazeCursor, Time.deltaTime * lerpRate);
    }

    void UpdatePreviousTarget()
    {
        for (int i = frozenObjects.Count-1; i >= 0; i--)
        {
            GameObject frozenObject = frozenObjects[i];

            if (frozenObject != null)
            {
                if (frozenObject.TryGetComponent(out Freezable freezable))
                {
                    if (unGazeTimers[i] <= gazeUnfreezeTime)
                    {
                        unGazeTimers[i] += Time.deltaTime;
                    }
                    else
                    {   //object is unfrozen
                        RuntimeManager.AttachInstanceToGameObject(unfreezeSound, frozenObject.transform);
                        unfreezeSound.start();

                        freezable.TriggerUnfreeze();
                        frozenObjects.RemoveAt(i);
                        unGazeTimers.RemoveAt(i);
                    }
                }
            }
        }
    }


    void UpdateGazeGraphics()
    {
        Vector2 gazeCursorScreen = cam.WorldToScreenPoint(gazeCursor);

        targetScale = (gazeTarget == null) ? unfocusScale : focusScale;
        currentScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * lerpRate);
        gazeImageRect.localScale = Vector3.one * currentScale;
        rippleEffects[0].transform.localScale = Vector3.one * currentScale * particleToCircleRatio;
        rippleEffects[1].transform.localScale = Vector3.one * currentScale * particleToCircleRatio;

        gazeImageRect.Rotate(-Vector3.forward, Time.deltaTime * 50f);

        gazeFillImages[0].fillAmount = maxFillAmount * gazeTimer / gazeResetTime;
        gazeFillImages[1].fillAmount = maxFillAmount * gazeTimer / gazeResetTime;
        
        Vector3 ripplePos = cam.ScreenToWorldPoint(new Vector3(gazeCursorScreen.x, gazeCursorScreen.y, -cam.transform.position.z / 2));
        rippleEffects[0].transform.position = ripplePos;
        rippleEffects[1].transform.position = ripplePos;
        
        gazeImageRect.anchoredPosition = gazeCursorScreen;
        gazeImageRect.anchoredPosition -= new Vector2(Screen.width / 2f, Screen.height / 2f);
        gazeImageRect.anchoredPosition *= 1 / (gazeSlope * Screen.height / 1080f);
        
        if (gazeTarget != null && frozenObjects.Contains(gazeTarget))
        {
            rippleEffects[1].GetComponent<ParticleSystem>().Play();
            rippleEffects[0].GetComponent<ParticleSystem>().Stop();
            rippleEffects[0].GetComponent<ParticleSystem>().Clear();
        }
        else
        {
            rippleEffects[0].GetComponent<ParticleSystem>().Play();
            rippleEffects[1].GetComponent<ParticleSystem>().Stop();
            rippleEffects[1].GetComponent<ParticleSystem>().Clear();
        }
    }

    public void ResetAll()
    {
        gazeTarget = null;
        gazeTimer = 0f;

        frozenObjects.Clear();
        unGazeTimers.Clear();
    }
    private Vector3 ProjectToPlaneInWorld(GazePoint gazePoint, float visualizationDistance)
    {
        Vector3 gazeOnScreen = gazePoint.Screen;
        gazeOnScreen += (transform.forward * visualizationDistance);
        return cam.ScreenToWorldPoint(gazeOnScreen);
    }
}