using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Utility class for parsing scene names and grouping them by level.
/// Scene names should follow the pattern: "LevelNumber-SceneIndex [OptionalText]" (e.g., "1-1 Terry", "1-2 Terry", "2-0 Meri").
/// </summary>
public static class LevelParser
{
    // Regex to match "digits-digits" at the start of a scene name
    private static readonly Regex LevelSceneRegex = new Regex(@"^(\d+)-(\d+)", RegexOptions.Compiled);
    
    /// <summary>
    /// Parses a scene name like "1-3 Terry" and returns the level number (1).
    /// Returns -1 if the scene name doesn't match the expected pattern.
    /// </summary>
    public static int GetLevelNumber(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return -1;
        
        // Remove file extension if present
        string name = System.IO.Path.GetFileNameWithoutExtension(sceneName);
        
        var match = LevelSceneRegex.Match(name);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int levelNumber))
            return levelNumber;
        
        return -1; // Not a level scene
    }
    
    /// <summary>
    /// Parses a scene name and returns the scene index within the level.
    /// Returns -1 if the scene name doesn't match the expected pattern.
    /// </summary>
    public static int GetSceneIndex(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return -1;
        
        string name = System.IO.Path.GetFileNameWithoutExtension(sceneName);
        
        var match = LevelSceneRegex.Match(name);
        if (match.Success && int.TryParse(match.Groups[2].Value, out int sceneIndex))
            return sceneIndex;
        
        return -1;
    }
    
    /// <summary>
    /// Groups all build scenes by level number.
    /// </summary>
    public static Dictionary<int, List<string>> GroupScenesByLevel()
    {
        var levels = new Dictionary<int, List<string>>();
        
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            int levelNumber = GetLevelNumber(sceneName);
            if (levelNumber > 0)
            {
                if (!levels.ContainsKey(levelNumber))
                    levels[levelNumber] = new List<string>();
                
                levels[levelNumber].Add(sceneName);
            }
        }
        
        // Sort scenes within each level by scene index
        foreach (var kvp in levels)
        {
            kvp.Value.Sort((a, b) => GetSceneIndex(a).CompareTo(GetSceneIndex(b)));
        }
        
        return levels;
    }
    
    /// <summary>
    /// Checks if a scene name follows the level scene naming pattern.
    /// </summary>
    public static bool IsLevelScene(string sceneName)
    {
        return GetLevelNumber(sceneName) > 0 && GetSceneIndex(sceneName) >= 0;
    }
}
