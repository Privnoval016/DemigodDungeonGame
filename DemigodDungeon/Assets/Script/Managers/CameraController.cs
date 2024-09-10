using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    public GameObject virtualCameraContainer;
    
    CinemachineVirtualCamera vCam;

    private CinemachineConfiner2D confiner;

    private string currentScene;

    public bool checkPositive = true;

    
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
        
        vCam = virtualCameraContainer.GetComponent<CinemachineVirtualCamera>();
        vCam.TryGetComponent(out confiner);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateScreenBounds(LevelManager.Instance.currentScene);
    }


    public void UpdateScreenBounds(string scene)
    {
        
        GameObject[] screenBounds = GameObject.FindGameObjectsWithTag("ScreenBounds");

        GameObject screenBound = null;
        
        foreach (GameObject s in screenBounds)
        {
            if (s.scene.name == scene)
            {
                screenBound = s;
                break;
            }
        }
        
        if (screenBound != null && screenBound.GetComponent<PolygonCollider2D>() != confiner.m_BoundingShape2D)
        {
            confiner.m_BoundingShape2D = screenBound.GetComponent<PolygonCollider2D>();
        }
        
        
    }

    public void BeginScreenShake(float intensity, Vector2 direction, float rate)
    {
        StartCoroutine(Shake(intensity, direction, rate));
    }

    public void BeginScreenShake(float intensity, float rate)
    {
        StartCoroutine(Shake(intensity, Random.insideUnitCircle, rate));
    }

    IEnumerator Shake(float intensity, Vector2 direction, float rate)
    {
        GameObject screenConfiner = confiner.m_BoundingShape2D.gameObject;

        Vector3 originalPos = screenConfiner.transform.position;
        screenConfiner.transform.position -= (Vector3)direction.normalized * intensity;

        while (screenConfiner != null && (screenConfiner.transform.position - originalPos).sqrMagnitude > 0.001)
        {
            screenConfiner.transform.position = Vector3.Lerp(screenConfiner.transform.position, originalPos, Time.deltaTime * rate);
            yield return null;
        }
        
        screenConfiner.transform.position = originalPos;
    }
}
