using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cinemachine;
using UnityEngine;

public class FreezeFallBlock : MonoBehaviour
{
    [SerializeField] private EventReference impactSoundRef;
    private EventInstance impactSound;
    private Rigidbody rb;
    private BoxCollider box;

    private bool falling = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        box = GetComponent<BoxCollider>();

        impactSound = RuntimeManager.CreateInstance(impactSoundRef);

        rb.isKinematic = true;
    }
    public void startFalling()
    {
        falling = true;
        rb.isKinematic = false;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (falling == true && collision.gameObject.layer == 6)
        {
            CameraController.Instance.BeginScreenShake(0.5f, new Vector2(Random.Range(-0.3f, 0.3f), -1), 10f);

            falling = false;
            rb.isKinematic = true;

            impactSound.start();
        }
    }
}
