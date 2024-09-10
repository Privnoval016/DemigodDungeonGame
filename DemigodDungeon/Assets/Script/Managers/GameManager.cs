using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    public GameObject player;
    
    public int numDeaths;
    
    public Dictionary<string, List<int>> collectibles;
    
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

        player ??= GameObject.FindGameObjectWithTag("Player");
        
        collectibles = SaveManager.Instance.gameData.collectibles;
        
        numDeaths = SaveManager.Instance.gameData.numDeaths;
        
        Time.timeScale = 1;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void AddCollectible(string scene, GameObject item)
    {
        CollectibleController collectibleController = item.GetComponent<CollectibleController>();
        List<int> collectiblesInSceneList = collectibles[scene];
        collectiblesInSceneList.Add(collectibleController.collectibleID);
        
        item.SetActive(false);

        if (collectibleController.returnPosition != Vector3.zero)
        {
            player.GetComponent<CharacterController>().Move(Vector3.zero);
            player.GetComponent<CharacterController>().enabled = false;

            player.transform.position = collectibleController.returnPosition;
            
            if (collectibleController.returnScene != LevelManager.Instance.currentScene)
            {
                LevelManager.Instance.CollectibleRespawn(collectibleController);
            }
            else
            {
                player.GetComponent<CharacterController>().enabled = true;
            }
        }
    }
    
    public void CreateCollectiblesList(string scene)
    {
        if (!collectibles.ContainsKey(scene))
            collectibles.Add(scene, new List<int>());
    }

    public void SpawnProperCollectibles(string sceneName)
    {
        GameObject[] collectiblesInScene = GameObject.FindGameObjectsWithTag("Collectible");
        
        foreach (GameObject collectible in collectiblesInScene)
        {
            CollectibleController collectibleController = collectible.GetComponent<CollectibleController>();
            if (collectibles[sceneName].Contains(collectibleController.collectibleID))
            {
                collectible.SetActive(false);
            }
        }
    }


    public int GetTotalCollectibleCount()
    {
        int total = 0;
        
        foreach (KeyValuePair<string, List<int>> entry in collectibles)
        {
            total += entry.Value.Count;
        }
        
        return total;
    }
}
