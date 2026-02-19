using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static class SaveSystem
{
    private static readonly string SAVE_FOLDER = Application.persistentDataPath + "/Saves/";
    private const string SAVE_FILE = "SaveData.json";

    private static string SaveFilePath => SAVE_FOLDER + SAVE_FILE;

    public static void Init()
    {
        // Create the save folder if it doesn't exist
        if (!Directory.Exists(SAVE_FOLDER))
        {
            Directory.CreateDirectory(SAVE_FOLDER);
        }
    }

    public static void SaveGame(PlayerData playerData)
    {
        Init();
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(SaveFilePath, json);
        Debug.Log("Game saved to: " + SaveFilePath);
    }

    public static PlayerData LoadGame()
    {
        Init();
        if (File.Exists(SaveFilePath))
        {
            string json = File.ReadAllText(SaveFilePath);
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(json);
            return playerData;
        }
        else
        {
            Debug.LogWarning("Save file not found: " + SaveFilePath);
            return null;
        }
    }

    public static bool HasSaveData()
    {
        Init();
        return File.Exists(SaveFilePath);
    }
}