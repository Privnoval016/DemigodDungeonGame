using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AnimationEvent : MonoBehaviour
{
    
    [SerializeField] private EventReference walkSoundRef;   
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void PlayStepSound()
    {
        StartCoroutine(PlayOneShotAsync(walkSoundRef, transform.position, 0.04f, 4));
    }

    IEnumerator PlayOneShotAsync(EventReference soundRef, Vector3 position, float delay, int numLoops)
    {
        for (int i = 0; i < numLoops; i++)
        {
            RuntimeManager.PlayOneShot(soundRef, position);
            yield return new WaitForSeconds(delay);
        }
    }
    
    
}
