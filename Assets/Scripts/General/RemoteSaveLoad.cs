using UnityEngine;

public class RemoteSaveLoad : MonoBehaviour
{
    /// <summary>
    /// Loads the save file.
    /// </summary>
    public void LoadGame()
    {
        Debug.Log("Attempting to load game remotely...");
        GameManager.Instance?.LoadGame();
    }

    /// <summary>
    /// Saves the game to the save file.
    /// </summary>
    public void SaveGame()
    {
        Debug.Log("Attempting to save game remotely...");
        GameManager.Instance?.SaveGame();
    }
}
