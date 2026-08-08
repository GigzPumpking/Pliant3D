#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class SaveDataClearer
{
    // WARNING: Change this string to match the exact filename your SaveSystem.cs uses!
    // Common examples: "/save.json", "/playerData.dat", "/savefile.save"
    private const string SAVE_FILE_NAME = "/save.json"; 

    [MenuItem("Tools/Pliant3D/Save Data/Delete Save File")]
    public static void DeleteSaveFile()
    {
        // 1. Target the specific file
        string path = Application.persistentDataPath + SAVE_FILE_NAME;

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"<color=green>SUCCESS:</color> Save file deleted successfully from: {path}");
        }
        else
        {
            Debug.LogWarning($"<color=yellow>NOTICE:</color> No save file found to delete at: {path}");
        }

        // 2. Clear PlayerPrefs (Highly recommended as many Unity settings default to here)
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("PlayerPrefs have also been cleared.");
    }

    [MenuItem("Tools/Pliant3D/Save Data/Open Save Folder")]
    public static void OpenSaveFolder()
    {
        // Opens the OS file explorer directly to the hidden persistentDataPath
        EditorUtility.RevealInFinder(Application.persistentDataPath);
        Debug.Log($"Opening folder: {Application.persistentDataPath}");
    }
}
#endif