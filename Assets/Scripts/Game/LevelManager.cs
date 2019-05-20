﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class LevelData {
    public int TotalPots;
    public StringBoolDictionary CollectedPots = new StringBoolDictionary();
    public StringBoolDictionary SpecialPots = new StringBoolDictionary();
    public bool IsCompleted;
}

public class LevelManager : MonoBehaviour {

    public string PersistentSceneName = "Persistent";
    public string StartingSceneName = "Hub";

    //public StringLevelDictionary Levels = new StringLevelDictionary();

    public static LevelManager Instance;

    // Start is called before the first frame update
    void Awake() {
        if(Instance != null) { Destroy(this.gameObject); return; }
        Instance = this;
    }

    private void Start() {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (SceneManager.GetActiveScene().name == PersistentSceneName) {
            LoadScene(StartingSceneName);
        }

        Game.Instance.playerStats.OnLoad += OnStatsLoad;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        print(scene.name + " was Loaded!");

        Player.Instance.gameObject.SetActive(false);
        CheckPointSystem.Instance.LoadStartPoint();
        Player.Instance.health.Reset();
        Player.Instance.gameObject.SetActive(true);

        AudioManager.Instance.PlaySceneMusic(scene.name);

        if (!Game.Instance.playerStats.Levels.ContainsKey(scene.name)) {
            Game.Instance.playerStats.Levels[scene.name] = new LevelData();
        }
    }

    private void OnStatsLoad(PlayerStats stats) {
        LevelData level = null;
        if (!stats.Levels.TryGetValue("Tutorial", out level)){
            LoadScene("Tutorial");
        } else {
            LoadScene("Hub");
        }
    }

    public void MoveToScene(GameObject obj) {
        SceneManager.MoveGameObjectToScene(obj, SceneManager.GetActiveScene());
    }

    public void RestartLevel(bool async = false) {
        if (async) {
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        } else {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }       
    }
    public void LoadLevel(int world, int level, bool async = false) {
        if (async) {
            SceneManager.LoadSceneAsync(world + "-" + level);
        } else {
            SceneManager.LoadScene(world + "-" + level);
        }
    }
    public void LoadHub(bool async = false) {  
        if (async) {
            SceneManager.LoadSceneAsync("Hub");
        } else {
            SceneManager.LoadScene("Hub");
        }
    }
    public void LoadScene(string scene, bool async = false) {    
        if (async) {
            SceneManager.LoadSceneAsync(scene);
        } else {
            SceneManager.LoadScene(scene);
        }
    }

    public string GetLevelName() {
        return SceneManager.GetActiveScene().name;
    }
    public int GetWorld() {
        int world = 0;
        switch (GetLevelName()) {
            case "1-1":
            case "1-2":
            case "1-3":
            case "Tutorial":
            case "Hub":
                world = 1;
                break;
            case "2-1":
            case "2-2":
            case "2-3":
                world = 2;
                break;
            case "3-1":
            case "3-2":
            case "3-3":
                world = 3;
                break;
            case "4-1":
            case "4-2":
            case "4-3":
                world = 4;
                break;
        }
        return world;
    }
    public int GetLevel() {
        int level = 0;
        switch (GetLevelName()) {
            case "Hub":
                level = 0;
                break;
            case "1-1":
            case "2-1":
            case "3-1":
            case "4-1":
            case "tutorial":
                level = 1;
                break;
            case "1-2":
            case "2-2":
            case "3-2":
            case "4-2":
                level = 2;
                break;
            case "1-3":
            case "2-3":
            case "3-3":
            case "4-3":
                level = 3;
                break;
        }
        return level;
    }
}