using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CutsceneCamera : MonoBehaviour
{
    public static CutsceneCamera Instance { get; private set; }
    
    [SerializeField] private GameObject transformContainer;
    [SerializeField] private Transform[] cutscenePoints;
    [SerializeField] private float[] cutsceneDurations;


    [SerializeField] private GameObject arachne;
    [SerializeField] private GameObject arachneContainer;
    [SerializeField] private Transform[] arachnePoints;
    [SerializeField] private float[] arachneDurations;

    [SerializeField] private Animator anim;
    [SerializeField] private int[] animTriggerFrames;

    [SerializeField] private float cameraBobAmplitude = 1f;
    [SerializeField] private float cameraBobFrequency = 1f;

    [SerializeField] private Volume[] volumes;
    
    
    [SerializeField] private float zoomDuration = 1f;
    [SerializeField] private float zoomDistance = 1f;
    [SerializeField] private Image transitionImage;
    
    private Camera cam;
    

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
        
        if (transformContainer != null)
        {
            cutscenePoints = new Transform[transformContainer.transform.childCount];
            foreach (Transform child in transformContainer.transform)
            {
                cutscenePoints[child.GetSiblingIndex()] = child;
            }
        }
        
        if (arachneContainer != null)
        {
            arachnePoints = new Transform[arachneContainer.transform.childCount];
            foreach (Transform child in arachneContainer.transform)
            {
                arachnePoints[child.GetSiblingIndex()] = child;
            }
        }
        
        transform.position = cutscenePoints[0].position;
        transform.rotation = cutscenePoints[0].rotation;
            
        arachne.transform.position = arachnePoints[0].position;
        arachne.transform.rotation = arachnePoints[0].rotation;
        
        volumes[0].weight = 1;
        volumes[1].weight = 0;
        
        transitionImage.gameObject.SetActive(true);
        transitionImage.color = new Color(255, 255, 255, 0);
        
        cam = GetComponent<Camera>();
        cam.fieldOfView = 60;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void StartCutscene()
    {
        Time.timeScale = 1.5f;
        StartCoroutine(Cutscene());
    }

    IEnumerator Cutscene()
    {
        transform.position = cutscenePoints[0].position;
        transform.rotation = cutscenePoints[0].rotation;

        for (int i = 1; i < cutscenePoints.Length; i++)
        {
            float elapsedTime = 0;
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            
            while (elapsedTime < cutsceneDurations[i])
            {
                if (i == cutscenePoints.Length - 1)
                {
                    volumes[0].weight = Mathf.Lerp(1, 0, elapsedTime / cutsceneDurations[i]);
                    volumes[1].weight = Mathf.Lerp(0, 1, elapsedTime / cutsceneDurations[i]);
                    cam.fieldOfView = Mathf.Lerp(60, 90, elapsedTime / cutsceneDurations[i]);
                }
                
                transform.position =
                    Vector3.Lerp(startPos, cutscenePoints[i].position, elapsedTime / cutsceneDurations[i]);
                transform.rotation = Quaternion.Lerp(startRot, cutscenePoints[i].rotation,
                    elapsedTime / cutsceneDurations[i]);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        
        StartCoroutine(MoveArachne());
        
        float time = 0;
        transform.position -= new Vector3(0, cameraBobAmplitude, 0);
        while (true)
        {
            time += Time.deltaTime; 
            transform.position += new Vector3(0, Mathf.Sin(time * cameraBobFrequency) * cameraBobAmplitude, 0);
            yield return null;
        }
    }

    IEnumerator MoveArachne()
    {
        arachne.transform.position = arachnePoints[0].position;
        arachne.transform.rotation = arachnePoints[0].rotation;
        
        
        for (int i = 1; i < arachnePoints.Length; i++)
        {
            if (animTriggerFrames.Contains(i))
            {
                anim.SetBool("Move", !anim.GetBool("Move"));
            }
            
            float elapsedTime = 0;
            Vector3 startPos = arachne.transform.position;
            Quaternion startRot = arachne.transform.rotation;
            while (elapsedTime < arachneDurations[i])
            {
                arachne.transform.position = Vector3.Lerp(startPos, arachnePoints[i].position, elapsedTime / arachneDurations[i]);
                arachne.transform.rotation = Quaternion.Lerp(startRot, arachnePoints[i].rotation, elapsedTime / arachneDurations[i]);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        
        StartCoroutine(BeginGame());
    }

    IEnumerator BeginGame()
    {
        float time = 0;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + transform.forward * zoomDistance;
        while (time < zoomDuration)
        {
            transitionImage.color = new Color(255, 255, 255, Mathf.Lerp(0, 1, time / zoomDuration));
            transform.position = Vector3.Lerp(startPos, targetPos, time / zoomDuration);
            time += Time.deltaTime;
            yield return null;
        }
        
        SceneManager.LoadScene(1);
    }

    IEnumerator FadeInTransition(float duration)
    {
        float time = 0;
        while (time < duration)
        {
            transitionImage.color = new Color(255, 255, 255, Mathf.Lerp(0, 1, time / duration));
            time += Time.deltaTime;
            yield return null;
        }
    }
}

