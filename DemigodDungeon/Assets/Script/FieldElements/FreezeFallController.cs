using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreezeFallController : Freezable
{
    public GameObject blockToDrop;

    [SerializeField] private EventReference crumbleSoundRef;
    private EventInstance crumbleSound;

    private FreezeFallBlock blockScript;
    
    // Start is called before the first frame update
    void Start()
    {
        blockToDrop.TryGetComponent(out blockScript);
        gameObject.GetComponent<BoxCollider>().enabled = true;

        crumbleSound = RuntimeManager.CreateInstance(crumbleSoundRef);

        lockPosition = transform.TransformPoint(GetComponent<BoxCollider>().center);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    protected override void ActivateFreeze()
    {
        
        petrification.Petrify();

        Invoke(nameof(Crumble), 0.4f);


        crumbleSound.start();
    }
    
    private void Crumble()
    {
        gameObject.GetComponent<BoxCollider>().enabled = false;
        
        GameObject crumbleParticle = Instantiate(GazeTracker.Instance.petrifyEffect, GetComponent<BoxCollider>().center, Quaternion.identity);
        Destroy(crumbleParticle, 1.5f);
        
        blockScript.startFalling();
        gameObject.SetActive(false);
    }
    
    protected override void ActivateUnfreeze()
    {
    }
}
