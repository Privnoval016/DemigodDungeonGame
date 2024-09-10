using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI deathText;
    
    [SerializeField] private Image endGameImage;
    [SerializeField] private float endGameSpeed;

    [Header("Respawn Animation")] 
    [SerializeField] private RectTransform[] deathScreens;

    [SerializeField] private RectTransform deathParticle;
    [SerializeField] private RectTransform collectibleParticle;
    
    [SerializeField] private float[] deathScreenStartXPositions;
    [SerializeField] private float[] deathScreenEndXPositions;
    [SerializeField] private float deathScreenSpeed;
    
    [SerializeField] private float particleGrowSpeed;
    
    [FormerlySerializedAs("isRespawning")] public bool playRespawning = false;
    public bool playCoinReset = false;

    private Coroutine respawnCoroutine, coinResetCoroutine, gameEndCoroutine;
    public bool gameOver = false;
    
    [HideInInspector] public CollectibleController curCollectible;

    [SerializeField] private EventReference transitionSoundRef;
    [SerializeField] private EventReference deathSoundRef;
    //[SerializeField] private EventReference respawnSoundRef;
    private EventInstance transitionSound;
    private EventInstance deathSound;
    //private EventInstance respawnSound;
    
    private Vector3 playerVelocity;
    
    // Start is called before the first frame update
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        deathParticle.gameObject.SetActive(false);
        collectibleParticle.gameObject.SetActive(false);

        transitionSound = RuntimeManager.CreateInstance(transitionSoundRef);
        deathSound = RuntimeManager.CreateInstance(deathSoundRef);
        //respawnSound = RuntimeManager.CreateInstance(respawnSoundRef);
        
        endGameImage.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (playRespawning)
        {
            respawnCoroutine ??= StartCoroutine(RespawnAnimation());
        }
        
        if (playCoinReset)
        {
            coinResetCoroutine ??= StartCoroutine(CoinResetAnimation());
        }
        
        Vector3 vel = GameManager.Instance.player.GetComponent<CharacterController>().velocity;
        
        if (vel.x != 0 && !playRespawning)
        {
            playerVelocity = vel;
        }

        coinText.text = GameManager.Instance.GetTotalCollectibleCount().ToString();
        deathText.text = GameManager.Instance.numDeaths.ToString();
    }

    IEnumerator RespawnAnimation()
    {
        LevelManager.Instance.isRespawning = true;
        
        GameObject player = GameManager.Instance.player;
        
        Vector3 lastPosition = player.transform.position;
        
        player.GetComponent<CharacterController>().Move(Vector3.zero); 
        
        player.GetComponent<CharacterController>().enabled = false;
        
        player.SetActive(false);
        
        deathParticle.localScale = Vector3.zero;
        deathParticle.gameObject.SetActive(true);

        deathParticle.transform.position = new Vector3(lastPosition.x, lastPosition.y, deathParticle.position.z);

        deathSound.start();

        bool startReseting = true;
        
        if (playerVelocity.x >= 0)
        {
            transitionSound.setParameterByName("Direction", 1); //pan right
            transitionSound.start();
            GlobalAudio.Instance.setMusicCutoff(1); //full cutoff

            deathScreens[0].anchoredPosition = new Vector2(deathScreenStartXPositions[0], deathScreens[0].anchoredPosition.y);
            
            while (deathScreens[0].anchoredPosition.x < deathScreenEndXPositions[0])
            {
                deathScreens[0].anchoredPosition += new Vector2(deathScreenSpeed * Time.deltaTime, 0);
                
                if (deathScreens[0].anchoredPosition.x > 0)
                {
                    player.SetActive(true);
                    
                    player.transform.position = LevelManager.Instance.curRespawnPoint;
                    
                    if (startReseting)
                    {
                        startReseting = false;
                        LevelManager.Instance.ResetLevel();
                    }
                }
                else
                {
                    player.transform.position = lastPosition;
                }
                
                GrowParticle(deathParticle);
                
                yield return null;
            }
        }
        else
        {
            transitionSound.setParameterByName("Direction", 0); //pan left
            transitionSound.start();
            GlobalAudio.Instance.setMusicCutoff(1); //full cutoff

            deathScreens[1].anchoredPosition = new Vector2(deathScreenStartXPositions[1], deathScreens[1].anchoredPosition.y);
            
            while (deathScreens[1].anchoredPosition.x > deathScreenEndXPositions[1])
            {
                deathScreens[1].anchoredPosition -= new Vector2(deathScreenSpeed * Time.deltaTime, 0);
                
                if (deathScreens[1].anchoredPosition.x < 0)
                {
                    player.SetActive(true);
                    
                    player.transform.position = LevelManager.Instance.curRespawnPoint;
                    
                    if (startReseting)
                    {
                        startReseting = false;
                        LevelManager.Instance.ResetLevel();
                    }
                }
                else
                {
                    player.transform.position = lastPosition;
                }
                
                GrowParticle(deathParticle);
                
                yield return null;
            }
        }
        
        player.GetComponent<CharacterController>().enabled = true;
        
        deathParticle.gameObject.SetActive(false);
        
        LevelManager.Instance.isRespawning = false;
        
        playRespawning = false;
        
        respawnCoroutine = null;

        GlobalAudio.Instance.setMusicCutoff(0);
        //respawnSound.start();
    }
    
    private void GrowParticle(RectTransform particle)
    {
        particle.gameObject.SetActive(true);
        particle.localScale += Vector3.one * particleGrowSpeed * Time.deltaTime;
    }
    
    IEnumerator CoinResetAnimation()
    {
        GameObject player = GameManager.Instance.player;
        
        Vector3 lastPosition = player.transform.position;
        
        player.GetComponent<CharacterController>().Move(Vector3.zero); 
        
        player.GetComponent<CharacterController>().enabled = false;
        
        
        collectibleParticle.localScale = Vector3.zero;
        collectibleParticle.gameObject.SetActive(true);


        collectibleParticle.transform.position = new Vector3(curCollectible.transform.position.x, 
            curCollectible.transform.position.y, collectibleParticle.position.z);


        bool startReseting = true;

        deathSound.start();

        if (playerVelocity.x >= 0)
        {
            transitionSound.setParameterByName("Direction", 1); //pan right
            transitionSound.start();

            deathScreens[0].anchoredPosition = new Vector2(deathScreenStartXPositions[0], deathScreens[0].anchoredPosition.y);
            
            while (deathScreens[0].anchoredPosition.x < deathScreenEndXPositions[0])
            {
                deathScreens[0].anchoredPosition += new Vector2(deathScreenSpeed * Time.deltaTime, 0);
                
                if (deathScreens[0].anchoredPosition.x > 0)
                {
                    
                    player.transform.position = curCollectible.returnPosition;
                    
                    if (startReseting)
                    {
                        startReseting = false;
                        LevelManager.Instance.StartCoroutine(LevelManager.Instance.RespawnCollectible(curCollectible));
                    }
                }
                else
                {
                    player.transform.position = lastPosition;
                }
                
                GrowParticle(collectibleParticle);
                
                yield return null;
            }
        }
        else
        {
            transitionSound.setParameterByName("Direction", 0); //pan left
            transitionSound.start();

            deathScreens[1].anchoredPosition = new Vector2(deathScreenStartXPositions[1], deathScreens[1].anchoredPosition.y);
            
            while (deathScreens[1].anchoredPosition.x > deathScreenEndXPositions[1])
            {
                deathScreens[1].anchoredPosition -= new Vector2(deathScreenSpeed * Time.deltaTime, 0);
                
                if (deathScreens[1].anchoredPosition.x < 0)
                {
                    
                    player.transform.position = curCollectible.returnPosition;
                    
                    if (startReseting)
                    {
                        startReseting = false;
                        LevelManager.Instance.StartCoroutine(LevelManager.Instance.RespawnCollectible(curCollectible));
                    }
                }
                else
                {
                    player.transform.position = lastPosition;
                }
                
                GrowParticle(collectibleParticle);
                
                yield return null;
            }
        }
        
        player.GetComponent<CharacterController>().enabled = true;
        
        collectibleParticle.gameObject.SetActive(false);
        
        playCoinReset = false;
        
        coinResetCoroutine = null;

        //respawnSound.start();
    }

    public void EndGame()
    {
        gameOver = true;
        gameEndCoroutine ??= StartCoroutine(EndGameAnimation());
    }
    
    IEnumerator EndGameAnimation()
    {
        endGameImage.gameObject.SetActive(true);
        endGameImage.color = new Color(255, 255, 255, 0);
        
        float timer = 0;
        
        while (timer < endGameSpeed)
        {
            timer += Time.deltaTime;
            endGameImage.color = new Color(255, 255, 255, Mathf.Lerp(0, 1, timer / endGameSpeed));
            yield return null;
        }
        
        Time.timeScale = 0;
        
        while (!Keyboard.current.anyKey.wasPressedThisFrame)
        {
            yield return null;
        }

        SceneManager.LoadScene(0);
    }
    
}
