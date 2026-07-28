using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Represents a logical level that spans multiple scenes (e.g., Level 1 = scenes 1-1, 1-2, 1-3, 1-4).
/// </summary>
public enum LevelId
{
    Level1 = 1,
    Level2 = 2,
    Level3 = 3,
    Level4 = 4,
    Level5 = 5,
    Level6 = 6,
}

/// <summary>
/// ScriptableObject that defines the data for a single logical level across multiple scenes.
/// Create instances via: Assets > Create > Levels > Level Data
/// </summary>
[CreateAssetMenu(fileName = "NewLevelData", menuName = "Levels/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Identity")]
    public LevelId levelId;
    public string displayName = "Level 1";
    
    [Header("Scene Configuration")]
    [Tooltip("Ordered list of scene names that make up this level (e.g., 1-1, 1-2, 1-3, 1-4)")]
    public List<string> sceneNames = new List<string>();
    
    [Header("Progress Tracking")]
    public int totalTasks = 0;
    public bool isCompleted = false;
    
    [Header("Audio")]
    public AudioData levelTheme;
    public AudioData levelAmbience;
    
    /// <summary>
    /// Returns the current scene index for this level based on the active scene.
    /// Returns -1 if the active scene doesn't belong to this level.
    /// </summary>
    public int GetCurrentSceneIndex()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        return sceneNames.IndexOf(activeScene);
    }
    
    /// <summary>
    /// Returns true if the active scene is part of this level.
    /// </summary>
    public bool IsActiveLevel()
    {
        return GetCurrentSceneIndex() >= 0;
    }
    
    /// <summary>
    /// Gets the next scene in this level, or null if at the end.
    /// </summary>
    public string GetNextScene()
    {
        int currentIndex = GetCurrentSceneIndex();
        if (currentIndex >= 0 && currentIndex < sceneNames.Count - 1)
            return sceneNames[currentIndex + 1];
        return null;
    }
    
    /// <summary>
    /// Gets the previous scene in this level, or null if at the start.
    /// </summary>
    public string GetPreviousScene()
    {
        int currentIndex = GetCurrentSceneIndex();
        if (currentIndex > 0)
            return sceneNames[currentIndex - 1];
        return null;
    }
    
    /// <summary>
    /// Gets progress through the current level (0.0 to 1.0) based on active scene.
    /// </summary>
    public float GetProgress()
    {
        if (sceneNames == null || sceneNames.Count == 0)
            return 0f;
        
        int currentIndex = GetCurrentSceneIndex();
        if (currentIndex < 0)
            return 0f;
        
        return (float)(currentIndex + 1) / sceneNames.Count;
    }
    
    /// <summary>
    /// Returns true if the given scene name belongs to this level.
    /// </summary>
    public bool ContainsScene(string sceneName)
    {
        return sceneNames != null && sceneNames.Contains(sceneName);
    }
}
