using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button : MonoBehaviour
{
    [SerializeField] private GameObject[] doors;
    [SerializeField] private bool isPressed = false;
    [SerializeField] private GameObject button;
    //[SerializeField] private float buttonAnimTime = 0.5f; //seconds
    //[SerializeField] private float doorAnimTime = 1f; //seconds

    [Header("FMOD Sound")]
    [SerializeField] private EventReference doorSoundRef;

    private Vector3 buttonUp;
    private Vector3 buttonDown;
    
    private Vector3[] doorsUp;
    private Vector3[] doorsDown;

    private float timer = 0;

    private EventInstance doorSound;
    
    private bool isPlaying = false;
    
    public float[] doorMoveOffset;
    
    // Start is called before the first frame update
    void Start()
    {
        doorsDown = new Vector3[doors.Length];
        doorsUp = new Vector3[doors.Length];
        
        buttonUp = button.transform.position;
        buttonDown = new Vector3(button.transform.position.x, button.transform.position.y - 0.15f, button.transform.position.z);

        for (int i = 0; i < doors.Length; i++)
        {
            doorsUp[i] = doors[i].transform.position;
            doorsDown[i] = new Vector3(doors[i].transform.position.x, doors[i].transform.position.y + doorMoveOffset[i],
                doors[i].transform.position.z);
        }

        doorSound = RuntimeManager.CreateInstance(doorSoundRef);
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < doors.Length; i++)
        {
            if (isPressed)
            {
                button.transform.position = Vector3.Lerp(button.transform.position, buttonDown, Time.deltaTime * 5);
                doors[i].transform.position = Vector3.Lerp(doors[i].transform.position, doorsDown[i], Time.deltaTime * 2);
            }
            else
            {
                button.transform.position = Vector3.Lerp(button.transform.position, buttonUp, Time.deltaTime * 5);
                doors[i].transform.position = Vector3.Lerp(doors[i].transform.position, doorsUp[i], Time.deltaTime);
            }
        }

        timer += Time.deltaTime;
    }
    private float clampMap(float a, float l1, float r1, float l2, float r2)
    //proportionately remaps a value from one range to another
    {
        return Math.Clamp( (a - l1) * (r2 - l2) / (r1 -l1) + r2, 0, 1);
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.TryGetComponent(out Freezable freezable))
            
            if (freezable.isFrozen)
            {
                isPressed = true;
                //timer = 0;
                //doorSound.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                if (!isPlaying)
                {
                    doorSound.start();
                    isPlaying = true;
                    CameraController.Instance.BeginScreenShake(0.1f,10f);
                }
            }
            else
            {
                isPressed = false;
                isPlaying = false;
                //timer = 0;
                doorSound.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }
    }
}
