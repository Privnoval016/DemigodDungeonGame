using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using WaitForSeconds = UnityEngine.WaitForSeconds;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public Coroutine loadNextCoroutine, unloadLastCoroutine, reloadCoroutine, initialLoadCoroutine, collectibleRespawnCoroutine;

    [Header("Scene Management")]
    
    public int sceneCount;
    
    public SceneField initialScene;
    
    public string currentScene;
    
    public List<string> loadedScenes = new List<string>();

    public float deloadBuffer;
    
    public bool movingToNextScene = false;
    
    [Header("Respawn Management")]
    
    public Vector3 curRespawnPoint;
    
    public bool isRespawning = false;

    public GameObject screenBound;
    
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
        
        initialLoadCoroutine ??= StartCoroutine(BeginSceneLoading("Level" + SaveManager.Instance.gameData.currentLevel));
        
    }

    void Start()
    {
        //CameraController.Instance.UpdateScreenBounds(initialScene); 
    }

    void Update()
    {
    }
    
    IEnumerator BeginSceneLoading(string scene)
    {
        isRespawning = true;
        GazeTracker.Instance.ResetAll();
        
        loadedScenes.Add(scene);
        SceneManager.LoadScene(scene, LoadSceneMode.Additive);
        yield return new WaitForEndOfFrame();
        
        GameManager.Instance.CreateCollectiblesList(scene);
        GameManager.Instance.SpawnProperCollectibles(scene);
        
        GameObject player = GameManager.Instance.player;
        player.GetComponent<CharacterController>().enabled = false;
        
        GameObject[] respawnControllers = GameObject.FindGameObjectsWithTag("RespawnController");
        
        RespawnController respawnController = null;

        foreach (GameObject controller in respawnControllers)
        {
            if (controller.scene.name == scene)
            {
                respawnController = controller.GetComponent<RespawnController>();
                break;
            }
        }
        
        if (respawnController != null)
            SetRespawnPoint(respawnController.respawnPointOnReset.transform.GetChild(0).position);
        
        player.transform.position = curRespawnPoint;
        player.GetComponent<CharacterController>().enabled = true;
        
        isRespawning = false;
        initialLoadCoroutine = null;
    }
     
    public void LoadNextScene(string toLoad)
    {
        if (loadedScenes.Contains(toLoad)) return;
        
        loadNextCoroutine ??= StartCoroutine(LoadScene(toLoad));
    }
    
    public void UnloadLastScene(string toUnload)
    {
        unloadLastCoroutine ??= StartCoroutine(UnloadScene(toUnload));
    }
    
    IEnumerator LoadScene(string toLoad)
    {
        movingToNextScene = true;
        GazeTracker.Instance.ResetAll();
        
        loadedScenes.Add(toLoad);

        GameManager.Instance.player.GetComponent<CharacterController>().Move(Vector3.zero);
        GameManager.Instance.player.GetComponent<CharacterController>().enabled = false;
        
        AsyncOperation load = SceneManager.LoadSceneAsync(toLoad, LoadSceneMode.Additive);
        yield return load;
        
        GameManager.Instance.player.GetComponent<CharacterController>().enabled = true;
        
        GameManager.Instance.CreateCollectiblesList(toLoad);
        GameManager.Instance.SpawnProperCollectibles(toLoad);
        
        SaveManager.Instance.Save();
        
        movingToNextScene = false;
        loadNextCoroutine = null;
        
        GameManager.Instance.player.GetComponent<CharacterController>().enabled = true;
    }
    
    IEnumerator UnloadScene(string toUnload)
    {
        
        
        loadedScenes.Remove(toUnload);
        AsyncOperation unload = SceneManager.UnloadSceneAsync(toUnload);
        yield return unload;
        unloadLastCoroutine = null;
    }

    public void Respawn()
    {
        UIManager.Instance.playRespawning = true;
    }

    public void ResetLevel()
    {
        reloadCoroutine ??= StartCoroutine(ReloadScene()); 
    }
    

    IEnumerator ReloadScene()
    {
        GazeTracker.Instance.ResetAll();

        GameManager.Instance.numDeaths++;
        
        foreach (string scene in loadedScenes)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
            yield return unload;
            SceneManager.LoadScene(scene, LoadSceneMode.Additive);
        }
        
        GameManager.Instance.player.SetActive(true);
        yield return new WaitForSeconds(0.04f);
        GameManager.Instance.SpawnProperCollectibles(currentScene);
        GameManager.Instance.player.SetActive(false);
        
        SaveManager.Instance.Save();
        
        reloadCoroutine = null;
    }
    
    public void SetRespawnPoint(Vector3 point)
    {
        curRespawnPoint = point;
    }

    public void CollectibleRespawn(CollectibleController collectible)
    {
        UIManager.Instance.curCollectible = collectible;
        UIManager.Instance.playCoinReset = true;
    }
    
    public IEnumerator RespawnCollectible(CollectibleController collectible)
    {
        string toUnload = currentScene;
        string toLoad = collectible.returnScene;
        
        movingToNextScene = true;
        GazeTracker.Instance.ResetAll();
        
        loadedScenes.Add(toLoad);
        
        AsyncOperation load = SceneManager.LoadSceneAsync(toLoad, LoadSceneMode.Additive);
        yield return load;
        
        GameManager.Instance.CreateCollectiblesList(toLoad);
        GameManager.Instance.SpawnProperCollectibles(toLoad);
        
        movingToNextScene = false;
        
        GameObject[] respawnControllers = GameObject.FindGameObjectsWithTag("RespawnController");
        
        RespawnController respawnController = null;

        foreach (GameObject controller in respawnControllers)
        {
            if (controller.scene.name == toLoad)
            {
                respawnController = controller.GetComponent<RespawnController>();
                break;
            }
        }
        
        if (respawnController != null)
            SetRespawnPoint(respawnController.respawnPointOnReset.transform.GetChild(0).position);

        yield return new WaitForSeconds(1f);
        
        loadedScenes.Remove(toUnload);
        
        AsyncOperation unload = SceneManager.UnloadSceneAsync(toUnload);
        yield return unload;
        
        SaveManager.Instance.Save();
        
        collectibleRespawnCoroutine = null;
    }
}
