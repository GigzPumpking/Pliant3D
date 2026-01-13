using System.Collections.Generic;

[System.Serializable]
public class PlayerData
{
    // Player's last scene
    public string sceneName;

    // Player's position in the scene
    public float[] playerPosition; // Storing a Vector3 as a float array

    // Player's form
    public string playerForm; // e.g., "Terry", "Frog", "Bulldozer"

    // Game settings
    public GameSettings settings;

    // A way to store the state of multiple objects in the scene
    public Dictionary<string, ObjectState> objectStates;

    public PlayerData()
    {
        settings = new GameSettings();
        objectStates = new Dictionary<string, ObjectState>();
    }
}

[System.Serializable]
public class GameSettings
{
    public float masterVolume;
    public int resolutionWidth;
    public int resolutionHeight;
    public bool isFullscreen;
}

[System.Serializable]
public class ObjectState
{
    public float[] position;
    public float[] rotation;
    public bool isActive;
}