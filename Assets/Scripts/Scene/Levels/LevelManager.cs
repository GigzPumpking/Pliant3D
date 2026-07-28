using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime manager that tracks the current level and handles level/scene transitions.
/// Attach to a persistent GameObject in your first scene (e.g., GameManager).
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [Header("Level Data")]
    [Tooltip("Assign all LevelData assets here, or use LevelParser to populate automatically")]
    [SerializeField] private List<LevelData> allLevels = new List<LevelData>();
    
    [Header("Current State")]
    [SerializeField] private LevelData currentLevel;
    [SerializeField] private int currentSceneIndex = -1;
    
    // Events for other systems to subscribe to
    public event Action<LevelData> OnLevelChanged;
    public event Action<LevelData> OnLevelCompleted;
    public event Action<string> OnSceneChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        if (allLevels == null || allLevels.Count == 0)
            PopulateLevelsFromBuildSettings();
        // Determine current level from active scene
        UpdateCurrentLevel();
    }
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateCurrentLevel();
        OnSceneChanged?.Invoke(scene.name);
    }
    
    /// <summary>
    /// Updates the current level based on the active scene.
    /// </summary>
    public void UpdateCurrentLevel()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        
        foreach (var level in allLevels)
        {
            bool isNewLevel = false;
            int index = level.sceneNames.IndexOf(activeScene);
            if (index >= 0)
            {
                if (level != null && level != currentLevel) isNewLevel = true; 
                currentLevel = level;
                currentSceneIndex = index;
                if (isNewLevel) OnLevelChanged?.Invoke(level);
                return;
            }
        }
        
        // No level found (menu scene, etc.)
        currentLevel = null;
        currentSceneIndex = -1;
    }
    
    /// <summary>
    /// Gets the current level data.
    /// </summary>
    public LevelData GetCurrentLevel()
    {
        if (currentLevel == null)
            UpdateCurrentLevel();
        return currentLevel;
    }
    
    /// <summary>
    /// Gets the current scene index within the level.
    /// </summary>
    public int GetCurrentSceneIndex()
    {
        if (currentSceneIndex < 0)
            UpdateCurrentLevel();
        return currentSceneIndex;
    }
    
    //TODO: Ignore for now but possible usage if needed
    /// <summary>
    /// Advances to the next scene in the current level.
    /// Returns true if successful, false if at the last scene.
    /// </summary>
    public bool AdvanceToNextScene()
    {
        if (currentLevel == null) return false;
        
        string nextScene = currentLevel.GetNextScene();
        if (!string.IsNullOrEmpty(nextScene))
        {
            SceneManager.LoadScene(nextScene);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Loads a specific scene within the current level.
    /// </summary>
    public void LoadSceneInLevel(int sceneIndex)
    {
        if (currentLevel == null || sceneIndex < 0 || sceneIndex >= currentLevel.sceneNames.Count)
            return;
        
        SceneManager.LoadScene(currentLevel.sceneNames[sceneIndex]);
    }
    
    /// <summary>
    /// Marks the current level as completed.
    /// </summary>
    public void CompleteCurrentLevel()
    {
        if (currentLevel == null) return;
        
        currentLevel.isCompleted = true;
        OnLevelCompleted?.Invoke(currentLevel);
        
        Debug.Log($"Level {currentLevel.displayName} completed!");
    }
    
    /// <summary>
    /// Gets a level by its ID.
    /// </summary>
    public LevelData GetLevel(LevelId levelId)
    {
        return allLevels.Find(l => l.levelId == levelId);
    }
    
    /// <summary>
    /// Checks if a scene belongs to a level.
    /// </summary>
    public bool IsLevelScene(string sceneName, out LevelData level)
    {
        level = allLevels.Find(l => l.sceneNames.Contains(sceneName));
        return level != null;
    }
    
    /// <summary>
    /// Gets the total number of scenes in the current level.
    /// </summary>
    public int GetCurrentLevelSceneCount()
    {
        return currentLevel?.sceneNames.Count ?? 0;
    }
    
    /// <summary>
    /// Gets progress through the current level (0.0 to 1.0).
    /// </summary>
    public float GetCurrentLevelProgress()
    {
        if (currentLevel == null || currentLevel.sceneNames.Count == 0)
            return 0f;
        
        return currentLevel.GetProgress();
    }
    
    /// <summary>
    /// Populates the allLevels list by parsing scene names from build settings.
    /// Call this in Start() if you want dynamic level detection instead of manual assignment.
    /// </summary>
    public void PopulateLevelsFromBuildSettings()
    {
        allLevels.Clear();
        
        // Group scenes by level number
        Dictionary<int, List<string>> levelScenes = new Dictionary<int, List<string>>();
        
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            int levelNumber = LevelParser.GetLevelNumber(sceneName);
            if (levelNumber > 0)
            {
                if (!levelScenes.ContainsKey(levelNumber))
                    levelScenes[levelNumber] = new List<string>();
                
                levelScenes[levelNumber].Add(sceneName);
            }
        }
        
        // Sort scenes within each level and create LevelData assets at runtime
        foreach (var kvp in levelScenes)
        {
            kvp.Value.Sort((a, b) => LevelParser.GetSceneIndex(a).CompareTo(LevelParser.GetSceneIndex(b)));
            
            LevelData levelData = ScriptableObject.CreateInstance<LevelData>();
            levelData.levelId = (LevelId)kvp.Key;
            levelData.displayName = $"Level {kvp.Key}";
            levelData.sceneNames = kvp.Value;
            
            allLevels.Add(levelData);
        }
    }
}
