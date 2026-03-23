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

    // # Tasks completed
	public int numTasksCompleted;
    // Game settings
    public GameSettings settings;

    // A way to store the state of multiple objects in the scene
    public Dictionary<string, ObjectState> objectStates;

    // Objective completion & fetch progress
    public List<ObjectiveSaveState> objectiveStates;

    public PlayerData()
    {
        settings = new GameSettings();
        objectStates = new Dictionary<string, ObjectState>();
        objectiveStates = new List<ObjectiveSaveState>();
        
        if(GameManager.Instance != null) numTasksCompleted = GameManager.Instance.GetNumTasksCompleted();
        else numTasksCompleted = 0;
    }
}

[System.Serializable]
public class GameSettings
{
    public float masterVolume;
    public int resolutionWidth;
    public int resolutionHeight;
    public bool isFullscreen;
    public bool autoSave = true;
}

[System.Serializable]
public class ObjectState
{
    public float[] position;
    public float[] rotation;
    public bool isActive;
}

[System.Serializable]
public class ObjectiveSaveState
{
    public string objectiveName;
    public string description;
    public bool isComplete;
    public int numCompleted;
    public bool fetchedAll;
    public List<string> fetchedItemNames = new List<string>();
    public List<string> completedInteractableNames = new List<string>();
    // How many times the NPC that gave this objective had been talked to
    public int npcInteractionCount;
}