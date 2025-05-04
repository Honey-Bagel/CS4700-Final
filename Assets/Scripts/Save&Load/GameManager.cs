using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    // Events
    public static event Action OnSaveRequested;
    public static event Action OnLoadRequested;
    public static event Action OnLevelCompleted;
    public static event Action OnPlayerReady;
    public static event Action OnLevelSetupFinished;
    public static event Action<int> OnScrapCountChanged;
    
    // State properties
    [SerializeField]
    public int CurrentLevel { get; private set; }
    public int CurrentSaveSlot { get; private set; } = -1;
    [SerializeField]
    public int DeathCount { get; private set; }
    [SerializeField]
    public float Difficulty { get; private set; }
    public float TotalPlaytimeMinutes { get; private set; }
    private int _scrapCount;
    public int ScrapCount { get => _scrapCount; private set {
            if(_scrapCount != value) {
                _scrapCount = value;
                OnScrapCountChanged?.Invoke(_scrapCount);
            }
        }
    }
    public int ScrapTowardsTarget { get; private set; }
    public bool IsPlayerReady;

    [SerializeField]
    public int TargetScrapCount { get; private set; }
    
    [Header("Scene Names")]
    [SerializeField] private string mainGameScene = "MainGame";
    [SerializeField] private string restScene = "RestScene";
    [SerializeField] private string transitionScene = "TransitionScene";
    
    // Transition parameters
    private string _nextSceneName;
    private bool _requiresLevelGeneration;
    private bool _requiresSaveDataLoading;

    [SerializeField]
    public Dictionary<Upgrade, int> upgrades = new Dictionary<Upgrade, int>();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Listen for level generation completion
        Generator.OnLevelGenerationComplete += OnLevelGenerationComplete;
    }
    
    private void OnDestroy()
    {
        Generator.OnLevelGenerationComplete -= OnLevelGenerationComplete;
    }
    
    // Player notifies us it's ready
    public void SetPlayerReady()
    {
        IsPlayerReady = true;
        OnPlayerReady?.Invoke();
    }
    
    // Level control
    public void CompleteLevel()
    {
        // if(ScrapTowardsTarget < TargetScrapCount)
        // {
        //     Debug.LogWarning("Not enough scrap");
        //     return;
        // }
        CurrentLevel++;
        OnLevelCompleted?.Invoke();
        SaveGame();
        LoadSceneWithTransition(restScene, false, true);
    }

    public void LevelFailed()
    {
        // Delete the save and go back to main menu
        SaveSystem.DeleteSave(CurrentSaveSlot);
        LoadSceneWithTransition("MainMenu", false, false);
    }
    
    public void StartNextLevel()
    {
        ScrapTowardsTarget = 0;
        SaveGame();
        LoadSceneWithTransition(mainGameScene, true, true);
    }
    
    // Save/Load
    public void SaveGame()
    {
        TotalPlaytimeMinutes += Time.unscaledDeltaTime / 60f;
        OnSaveRequested?.Invoke();
        
        if(CurrentSaveSlot >= 0) {
            SaveSystem.SaveGame(CurrentSaveSlot);
        } else {
            Debug.LogError("No save slot selected. Cannot save game.");
        }
    }
    
    public void LoadGame()
    {
        if(CurrentSaveSlot >= 0 && SaveSystem.LoadGame(CurrentSaveSlot)) {
            OnLoadRequested?.Invoke();
        } else {
            Debug.LogError("Failed to load game or no save slot selected");
        }
    }
    
    // Level generation callback
    private void OnLevelGenerationComplete()
    {
        Debug.Log("Level generation complete, checking for player...");
        StartCoroutine(WaitToLoadSaveData());

        SetupLevel();
    }

    private void SetupLevel()
    {
        Debug.LogWarning("Setting up level...");
        float modifier = UnityEngine.Random.Range(0.8f, 1.2f);
        float difficultyFactor = 1.0f + (Difficulty * 0.1f);
        float levelFactor = 1.0f + (CurrentLevel * 0.05f);

        TargetScrapCount = Mathf.RoundToInt(300 * modifier * difficultyFactor * levelFactor);

        OnLevelSetupFinished?.Invoke();
    }
    
    private IEnumerator WaitToLoadSaveData()
    {
        // Wait a bit for the player to initialize
        yield return new WaitForSeconds(0.1f);
        
        if (_requiresSaveDataLoading)
        {
            LoadGame();
            _requiresSaveDataLoading = false;
        }
    }
    
    // Scene management
    public void LoadSceneWithTransition(string sceneName, bool needsGeneration, bool needsSaveLoading)
    {
        _nextSceneName = sceneName;
        _requiresLevelGeneration = needsGeneration;
        _requiresSaveDataLoading = needsSaveLoading;
        
        SceneManager.LoadScene(transitionScene);
    }
    
    // For transition scene to get parameters
    public string GetNextSceneName() => _nextSceneName;
    public bool NeedsLevelGeneration() => _requiresLevelGeneration;
    public bool NeedsSaveDataLoading() => _requiresSaveDataLoading;
    
    // Update state from saved data
    public void UpdateGameState(int level, int deaths, float playtime, int scrapCount)
    {
        CurrentLevel = level;
        DeathCount = deaths;
        TotalPlaytimeMinutes = playtime;
        ScrapCount = scrapCount;
    }

    // Add these methods to the GameManager class

    // Return the main game scene name
    public string GetMainGameSceneName()
    {
        return mainGameScene;
    }

    // Reset game state for a new game
    public void ResetGameState()
    {
        CurrentLevel = 1;
        DeathCount = 0;
        TotalPlaytimeMinutes = 0f;
    }

    public void AddScrap(int amount)
    {
        ScrapTowardsTarget += amount;
        ScrapCount += amount;
    }

    public bool RemoveScrap(int amount)
    {
        if (ScrapCount - amount < 0)
        {
            ScrapCount = 0;
            return false;
        }
        ScrapCount -= amount;
        return true;
    }

    public void SetCurrentSaveSlot(int slotId)
    {
        CurrentSaveSlot = slotId;
    }

    public void AddUpgrade(Upgrade type) {
        if(upgrades == null) {
            upgrades = new Dictionary<Upgrade, int>();
        }
        if (upgrades.ContainsKey(type)) {
            upgrades[type]++;
        } else {
            upgrades.Add(type, 1);
        }
    }

    public void LoadUpgrades(List<SerializableUpgrade> serializableUpgrades)
    {
        upgrades.Clear();

        foreach(var serializableUpgrade in serializableUpgrades)
        {
            Upgrade upgrade = Resources.FindObjectsOfTypeAll<Upgrade>().FirstOrDefault(u => u.name == serializableUpgrade.upgradeName);
            if(upgrade != null) {
                upgrades[upgrade] = serializableUpgrade.count;
            } else {
                Debug.LogWarning($"Could not find upgrade with name: {serializableUpgrade.upgradeName}");
            }
        }
        Debug.Log($"Loaded {upgrades.Count} upgrades");

        ApplyUpgrades();
    }

    public void ApplyUpgrades() {
        PlayerController player = FindObjectOfType<PlayerController>();
        if(player != null) {
            FindObjectOfType<PlayerController>().ApplyUpgrades(upgrades);
        }
    }
}