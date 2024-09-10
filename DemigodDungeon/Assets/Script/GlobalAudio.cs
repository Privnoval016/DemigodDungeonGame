using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalAudio : MonoBehaviour
{
    public static GlobalAudio Instance { get; private set; }

    [SerializeField] private EventReference voiceReverseRef;
    [SerializeField] private EventReference musicRef;
    public EventInstance voiceReverseSound { get; private set; }
    public EventInstance music { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this);
        }

        voiceReverseSound = RuntimeManager.CreateInstance(voiceReverseRef);
        music = RuntimeManager.CreateInstance(musicRef);
        setMusicCutoff(0); //no low pass filter
    }
    // Start is called before the first frame update
    void Start()
    {
        voiceReverseSound.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        if (SceneManager.GetActiveScene().name == "ManagerScene") transitionGameMusic();
        music.start();
    }
    [Tooltip("set the intensity of lowpass filter from 0 to 1")]
    public void setMusicCutoff(float intensity)
    {
        music.setParameterByName("Intensity",intensity);
    }
    public void transitionMenuMusic()
    {
        music.setParameterByName("Type", 0);
    }
    public void transitionGameMusic()
    {
        music.setParameterByName("Type", 1);
    }
    // Update is called once per frame
    void Update()
    {

    }
    void setVocalIntensity(float intensity)
    {
        
    }
}
