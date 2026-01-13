using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static class SaveSystem
{
    private static readonly string SAVE_FOLDER = Application.persistentDataPath + "/Saves/";
    private const string SAVE_EXTENSION = ".json";

    public static void Init()
    {
        // Create the save folder if it doesn't exist
        if (!Directory.Exists(SAVE_FOLDER))
        {
            Directory.CreateDirectory(SAVE_FOLDER);
        }
    }

    public static void SaveGame(string saveFileName, PlayerData playerData)
    {
        Init();
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(SAVE_FOLDER + saveFileName + SAVE_EXTENSION, json);
        Debug.Log("Game saved to: " + SAVE_FOLDER + saveFileName + SAVE_EXTENSION);
    }

    public static PlayerData LoadGame(string saveFileName)
    {
        Init();
        string filePath = SAVE_FOLDER + saveFileName + SAVE_EXTENSION;
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(json);
            Debug.Log("Game loaded from: " + filePath);
            return playerData;
        }
        else
        {
            Debug.LogWarning("Save file not found: " + filePath);
            return null;
        }
    }
}