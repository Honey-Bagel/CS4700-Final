using System;
using System.Collections;
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
    
    // State properties
    [SerializeField]
    public int CurrentLevel { get; private set; }
    [SerializeField]
    public int DeathCount { get; private set; }
    public float TotalPlaytimeMinutes { get; private set; }
    public bool IsPlayerReady { get; private set; }
    
    [Header("Scene Names")]
    [SerializeField] private string mainGameScene = "MainGame";
    [SerializeField] private string restScene = "RestScene";
    [SerializeField] private string transitionScene = "TransitionScene";
    
    // Transition parameters
    private string _nextSceneName;
    private bool _requiresLevelGeneration;
    private bool _requiresSaveDataLoading;
    
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
        NewGenerator.OnLevelGenerationComplete += OnLevelGenerationComplete;
    }
    
    private void OnDestroy()
    {
        NewGenerator.OnLevelGenerationComplete -= OnLevelGenerationComplete;
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
        CurrentLevel++;
        OnLevelCompleted?.Invoke();
        SaveGame();
        LoadSceneWithTransition(restScene, false, true);
    }
    
    public void StartNextLevel()
    {
        SaveGame();
        LoadSceneWithTransition(mainGameScene, true, true);
    }
    
    // Save/Load
    public void SaveGame()
    {
        TotalPlaytimeMinutes += Time.unscaledDeltaTime / 60f;
        OnSaveRequested?.Invoke();
        SaveSystem.SaveGame();
    }
    
    public void LoadGame()
    {
        if (SaveSystem.LoadGame())
        {
            OnLoadRequested?.Invoke();
        }
    }
    
    // Level generation callback
    private void OnLevelGenerationComplete()
    {
        Debug.Log("Level generation complete, checking for player...");
        StartCoroutine(WaitToLoadSaveData());
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
    public void UpdateGameState(int level, int deaths, float playtime)
    {
        CurrentLevel = level;
        DeathCount = deaths;
        TotalPlaytimeMinutes = playtime;
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
}