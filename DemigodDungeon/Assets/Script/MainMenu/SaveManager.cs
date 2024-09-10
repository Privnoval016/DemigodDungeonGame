using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public GameData gameData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
        
        Load();
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    public void ResetSave()
    {
        gameData = new GameData();
        gameData.currentLevel = 1;
        gameData.highestUnlockedLevelIndex = 1;
        gameData.collectibles = new Dictionary<string, List<int>>();
        gameData.numDeaths = 0;
        gameData.usingEyeTracker = true;
        Save();
    }

    public void Save()
    {
        gameData.numDeaths = GameManager.Instance.numDeaths;
        gameData.collectibles = GameManager.Instance.collectibles;
        gameData.currentLevel = int.Parse(LevelManager.Instance.currentScene.Substring(5));
        gameData.highestUnlockedLevelIndex = Mathf.Max(gameData.highestUnlockedLevelIndex, gameData.currentLevel);
        gameData.usingEyeTracker = GazeTracker.Instance.usingEyeTracker;
        
        string json = JsonUtility.ToJson(gameData);
        json += CollectiblesToString(gameData.collectibles);
        
        string filePath = Application.persistentDataPath + "/GameData.json";
        System.IO.File.WriteAllText(filePath, json);
        print ("Saved to " + filePath);
    }

    public void Load()
    {
        string filePath = Application.persistentDataPath + "/GameData.json";
        if (System.IO.File.Exists(filePath))
        {
            string json = System.IO.File.ReadAllText(filePath);
            string collectiblesString = json.Substring(json.IndexOf("//Collectibles//") + 16);
            gameData = JsonUtility.FromJson<GameData>(json.Substring(0, json.IndexOf("//Collectibles//")));
            gameData.collectibles = StringToCollectibles(collectiblesString);
            print ("Loaded from " + filePath);
        }
        else
        {
            gameData = new GameData();
            gameData.currentLevel = 1;
            gameData.highestUnlockedLevelIndex = 1;
            gameData.collectibles = new Dictionary<string, List<int>>();
            gameData.numDeaths = 0;
            print ("No save file found, creating new save file");
        }
    }
    
    public string CollectiblesToString(Dictionary<string, List<int>> collectibles)
    {
        string collectiblesString = "//Collectibles//";
        foreach (KeyValuePair<string, List<int>> entry in collectibles)
        {
            collectiblesString += entry.Key + ":";
            foreach (int collectible in entry.Value)
            {
                collectiblesString += collectible + ",";
            }
            collectiblesString = collectiblesString.Substring(0, collectiblesString.Length - 1);
            collectiblesString += ";";
        }
        collectiblesString = collectiblesString.Substring(0, collectiblesString.Length - 1);
        return collectiblesString;
    }
    
    public Dictionary<string, List<int>> StringToCollectibles(string collectiblesString)
    {
        Dictionary<string, List<int>> collectibles = new Dictionary<string, List<int>>();
        
        if (collectiblesString == "")
            return collectibles;
        
        string[] collectiblesArray = collectiblesString.Split(';');
        foreach (string collectible in collectiblesArray)
        {
            if (collectible == "")
                continue;
            
            string[] collectibleData = collectible.Split(':');
            string scene = collectibleData[0];
            
            if (collectibleData.Length == 1)
            {
                collectibles.Add(scene, new List<int>());
                continue;
            }
            
            string[] collectibleIDs = collectibleData[1].Split(',');
            
            List<int> collectibleList = new List<int>();
            foreach (string collectibleID in collectibleIDs)
            {
                
                collectibleList.Add(int.Parse(collectibleID));
            }
            
            collectibles.Add(scene, collectibleList);
        }
        
        return collectibles;
    }

    public class GameData
    {
        public int currentLevel;
        public int highestUnlockedLevelIndex;
        public Dictionary<string, List<int>> collectibles;
        public int numDeaths;
        public bool usingEyeTracker;
    }
}
