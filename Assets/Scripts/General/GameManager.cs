using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Linq.Expressions;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.Video;

public class GameManager : KeyActionReceiver<GameManager>
{
    [SerializeField] private GameObject gameOverPanel;
    public bool isGameOver = false;
    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }
    [SerializeField] private Transform player;
    private TransformationWheel transformWheel;

    [SerializeField] private AudioData mainTheme;
    [SerializeField] private AudioData Ambience;
    private int _queuedTasksCompleted = 0;
    private int _queuedTasksAssigned = 0;
    private int _numTasksCompleted = 0;
    private int _numTasksAssigned = 0;

    [SerializeField] private float promotionRatio = 0.6f;

    // Objective states pending restoration after a scene reload (game-over reset or save load)
    private List<ObjectiveSaveState> _pendingObjectiveStates;

    // Timer remaining time pending restoration after a scene reload
    private float _pendingTimerTime = -1f;

    // Set to true when a timer expiry caused the game over; suppresses state capture on reset
    private bool _timerFailed = false;

    // NPC trigger interaction states pending restoration after a scene reload
    private List<NpcTriggerSaveState> _pendingNpcStates;

    // Main menu scene name
    [SerializeField] private string mainMenuSceneName = "0 Main Menu";
    [SerializeField] private VideoPlayer outroVideoPlayer;

    // Scenes where saving is not allowed (transitions, main menu, etc.)
    [SerializeField] private List<string> unsaveableScenes = new List<string>
    {
        "0 Main Menu",
        "1-0 Terry",
        "2-0 Meri",
        "3-0 Jerry",
        "4-0 Carrie",
        "5-0 Perry",
        "11-0 Thanks"
    };

    /// <summary>
    /// Returns true if the current scene allows saving.
    /// </summary>
    public bool CanSave()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        return !unsaveableScenes.Contains(currentScene);
    }

    // Auto-save: on by default, persisted via PlayerPrefs
    private const string AUTO_SAVE_PREF_KEY = "AutoSaveEnabled";
    public bool AutoSaveEnabled
    {
        get { return PlayerPrefs.GetInt(AUTO_SAVE_PREF_KEY, 1) == 1; }
        set
        {
            PlayerPrefs.SetInt(AUTO_SAVE_PREF_KEY, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public AudioData GetMainTheme()
    {
        return mainTheme;
    }

    // Static key mapping shared across all GameManager instances.
    public static Dictionary<string, Action<GameManager, InputAction.CallbackContext>> staticKeyMapping =
        new Dictionary<string, Action<GameManager, InputAction.CallbackContext>>()
        {
            { "Pause", (manager, ctx) => manager.Pause(ctx) },
            { "Reset", (manager, ctx) => manager.Reset(ctx) }
        };

    protected override Dictionary<string, Action<GameManager, InputAction.CallbackContext>> KeyMapping => staticKeyMapping;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(this.gameObject);
            return;
        }
        DontDestroyOnLoad(this.gameObject);

        // Subscribe to new-scene events for auto-save
        EventDispatcher.AddListener<NewSceneLoaded>(OnNewSceneLoaded);
    }

    void Start()
    {
        if (AudioManager.Instance != null)
        {
            if (AudioManager.Instance.playOnAwake)
            {
                //Handle Music Carryover between scenes
                instance.mainTheme = this.mainTheme;
                instance.Ambience = this.Ambience;
                AudioManager.Instance?.DeleteCurrentMusicSources();
                AudioManager.Instance?.PlayMusic(mainTheme);
                AudioManager.Instance?.PlayMusic(Ambience);
            }
        }
    }

    public void SetPlayer(Transform player)
    {
        this.player = player;
    }

    public Transform GetPlayer()
    {
        if (player == null)
        {
            player = Player.Instance?.transform;
        }

        return player;
    }

    public void Pause()
    {
        UIManager.Instance?.Pause();
    }

    public void Pause(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Pause();
        }
    }

    public void Reset()
    {
        if (!_timerFailed)
        {
            // Capture objective progress before the scene is destroyed
            _pendingObjectiveStates = CaptureObjectiveStates();

            // Capture NPC trigger states before the scene is destroyed
            _pendingNpcStates = CaptureNpcTriggerStates();

            // Capture timer progress before the scene is destroyed
            var timer = FindObjectOfType<ObjectiveTimer>();
            if (timer != null && timer.HasStarted)
            {
                _pendingTimerTime = timer.GetCurrentTime();
                Debug.Log($"[GameManager.Reset] Captured timer: {_pendingTimerTime:F1}s");
            }
            else
                Debug.Log($"[GameManager.Reset] Timer not captured — found={timer != null}, hasStarted={timer?.HasStarted}");
        }
        else
        {
            // Timer failure: start fresh — wipe any saved state
            _pendingObjectiveStates = null;
            _pendingNpcStates = null;
            _pendingTimerTime = -1f;
            _timerFailed = false;
        }

        // Restart the game
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        try
        {
            Player.Instance?.resetPosition();
            Player.Instance?.SetTransformation(Transformation.TERRY);
            Player.Instance?.SetVelocity(Vector3.zero);
            Player.Instance?.canMoveToggle(true);
            isGameOver = false;
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            if (transformWheel == null) transformWheel = Player.Instance?.GetComponentInChildren<TransformationWheel>();
        }
        catch
        {
            Debug.LogError("Error loading dependencies when restarting scene");
        }
        var eventSystem = EventSystem.current;
        eventSystem.SetSelectedGameObject(null);
        UIManager.Instance?.DisableGameOverPanel();
    }

    public void GameOver()
    {
        if (isGameOver) return;
        UIManager.Instance?.GameOverProtocol();
        isGameOver = true;
    }

    public void Reset(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Reset();
        }
    }

    public void AddQueuedTaskComplete()
    {
        _queuedTasksCompleted++;
    }

    public void AddQueuedTaskAssigned()
    {
        _queuedTasksAssigned++;
    }
    
    public void SetNumTasksAssigned(int num)
    {
        _numTasksAssigned = num;
    }

    public void AddNumTasksAssigned()
    {
        _numTasksAssigned++;
    }
    
    public void AddNumTasksCompleted()
    {
        _numTasksCompleted++;
    }
    
    public void SetNumTasksCompleted(int num)
    {
        _numTasksCompleted = num;
    }

    public int GetNumTasksAssigned()
    {
        return _numTasksAssigned;
    }
    
    public int GetNumTasksCompleted()
    {
        return _numTasksCompleted;
    }
    
    public int GetNumTasksRemaining()
    {
        return _numTasksAssigned - _numTasksCompleted;
    }
    
    public float GetRatioOfTasksCompleted()
    {
        if (_numTasksAssigned == 0) return 0;
        return ((float)_numTasksCompleted / _numTasksAssigned );
    }
    
    public float GetPromotionRatio()
    {
        return Instance.promotionRatio;
    }

    public void ToggleAutoSave()
    {
        AutoSaveEnabled = !AutoSaveEnabled;
    }

    public void SetAutoSave(bool enabled)
    {
        AutoSaveEnabled = enabled;
    }

    #region Objective State Persistence

    /// <summary>
    /// Snapshots every Objective in the current scene so their completion /
    /// fetch-item state can be restored after a scene reload.
    /// </summary>
    public List<ObjectiveSaveState> CaptureObjectiveStates()
    {
        var states = new List<ObjectiveSaveState>();
        // Only capture objectives that are actively tracked in a listing.
        // This avoids capturing un-given NPC objectives that exist in the scene.
        foreach (var listing in FindObjectsOfType<ObjectiveListing>())
        {
            foreach (var obj in listing.objectives)
            {
                if (obj != null)
                    states.Add(obj.CaptureState());
            }
        }

        // Annotate saved objectives with their NPC's interaction count so
        // dialogue progression can be restored after a scene reload.
        foreach (var trigger in FindObjectsOfType<DialogueTrigger>())
        {
            if (!trigger.ObjectiveGiven) continue;
            var toGive = trigger.ObjectivesToGive;
            if (toGive == null || toGive.Count == 0) continue;

            int count = trigger.GetInteractionCount();
            foreach (var obj in toGive)
            {
                if (obj == null) continue;
                var saved = states.Find(
                    s => s.objectiveName == obj.gameObject.name
                      && s.description   == obj.description);
                if (saved != null)
                    saved.npcInteractionCount = count;
            }
        }

        return states;
    }

    public List<ObjectiveSaveState> GetPendingObjectiveStates()
    {
        return _pendingObjectiveStates;
    }

    public void ClearPendingObjectiveStates()
    {
        _pendingObjectiveStates = null;
    }

    public float GetPendingTimerTime() => _pendingTimerTime;

    public void ClearPendingTimerTime() => _pendingTimerTime = -1f;

    public void SetTimerFailed() => _timerFailed = true;

    private List<NpcTriggerSaveState> CaptureNpcTriggerStates()
    {
        var states = new List<NpcTriggerSaveState>();
        foreach (var trigger in FindObjectsOfType<DialogueTrigger>())
        {
            int count = trigger.GetInteractionCount();
            if (count > 0)
                states.Add(new NpcTriggerSaveState { npcName = trigger.gameObject.name, interactionCount = count });
        }
        return states;
    }

    public List<NpcTriggerSaveState> GetPendingNpcStates() => _pendingNpcStates;

    public void ClearPendingNpcStates() => _pendingNpcStates = null;

    #endregion

    private String prevSceneStr = "";
    private String currSceneStr = "";

    private void OnNewSceneLoaded(NewSceneLoaded e)
    {
        //IF A PREVIOUS SCENE HAS NOT BEEN SET,
        if (prevSceneStr == "") prevSceneStr = SceneManager.GetActiveScene().name;
        else prevSceneStr = currSceneStr;
        
        currSceneStr = SceneManager.GetActiveScene().name;

        Debug.LogWarning("Loaded ");
        //ONLY ADD COMPLETED TASKS WHEN THE PLAYER HAS FINISHED THE LEVEL THEY ARE ON
        //SO ENSURE THAT THE NEW SCENE THAT WAS LOADED WAS NOT THE OLD SCENE (i.e a Level Reset)

        if (prevSceneStr != currSceneStr)
        {
            _numTasksCompleted += _queuedTasksCompleted;
            _numTasksAssigned += _queuedTasksAssigned;
        }
        //RESET QUEUED
        _queuedTasksCompleted = 0;
        _queuedTasksAssigned = 0;


        if (!AutoSaveEnabled) return;

        // Don't auto-save on unsaveable scenes (main menu, transitions, etc.)
        if (unsaveableScenes.Contains(e.sceneName)) return;

        SaveGame();
        Debug.Log("Auto-saved on scene load: " + e.sceneName);
        
        if (SceneManager.GetActiveScene().name != "11 End Screen")
        {
            AudioManager.Instance?.StopMusic();
        }
        
        {
            Debug.LogWarning($"Scene loaded ({e.sceneName}) does not match active scene ({SceneManager.GetActiveScene().name})");
        }
    }

    private void OnDestroy()
    {
        EventDispatcher.RemoveListener<NewSceneLoaded>(OnNewSceneLoaded);
    }

    public void Quit()
    {
        if(outroVideoPlayer) StartCoroutine(nameof(QuitCoroutine),0f); // Delay to allow outro video to start playing
        else
        {
            Debug.Log("Outro Video Player is Null");
            Application.Quit();
        }
    }

    public IEnumerator QuitCoroutine()
    {
        Debug.Log("Playing Quit Coroutine");
        outroVideoPlayer.gameObject.SetActive(true);
        outroVideoPlayer.gameObject.transform.SetAsLastSibling();
        outroVideoPlayer.Play();
        yield return new WaitForSeconds((float)outroVideoPlayer.clip.length);
        Application.Quit();
    }
    
    public static void Quit(int exitCode)
    {
        Application.Quit();
    }

    public void MainMenu()
    {
        // Load the main menu scene
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void SaveGame()
    {
        if (!CanSave())
        {
            Debug.LogWarning("Save blocked — current scene is not saveable.");
            return;
        }

        PlayerData playerData = new PlayerData();
        playerData.sceneName = SceneManager.GetActiveScene().name;
        playerData.settings.autoSave = AutoSaveEnabled;
        playerData.objectiveStates = CaptureObjectiveStates();

        // Capture NPC trigger states
        playerData.npcTriggerStates = CaptureNpcTriggerStates();

        // Capture timer state if one is active in the scene
        var timer = FindObjectOfType<ObjectiveTimer>();
        if (timer != null && timer.HasStarted)
            playerData.timerTime = timer.GetCurrentTime();

        if (Player.Instance != null)
        {
            /*
            Vector3 playerPosition = Player.Instance.transform.position;
            playerData.playerPosition = new float[] { playerPosition.x, playerPosition.y, playerPosition.z };
            playerData.playerForm = Player.Instance.transformation.ToString();
            */
        }

        /*
        if (AudioManager.Instance != null)
        {
            playerData.settings.masterVolume = AudioManager.Instance.GetGlobalVolume();
        }

        playerData.settings.resolutionWidth = Screen.currentResolution.width;
        playerData.settings.resolutionHeight = Screen.currentResolution.height;
        playerData.settings.isFullscreen = Screen.fullScreen;
        */

        SaveSystem.SaveGame(playerData);
    }

    public void LoadGame()
    {
        PlayerData playerData = SaveSystem.LoadGame();
        if (playerData != null)
        {
            StartCoroutine(LoadSceneAndApplyData(playerData));
        }
    }

    private IEnumerator LoadSceneAndApplyData(PlayerData playerData)
    {
        // Set pending objective states BEFORE the scene loads
        _pendingObjectiveStates = playerData.objectiveStates;

        // Restore NPC trigger states if saved
        if (playerData.npcTriggerStates != null && playerData.npcTriggerStates.Count > 0)
            _pendingNpcStates = playerData.npcTriggerStates;

        // Restore timer state if it was saved
        if (playerData.timerTime > 0f)
            _pendingTimerTime = playerData.timerTime;

        // Load the scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(playerData.sceneName);

        // Wait until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Restore auto-save preference
        AutoSaveEnabled = playerData.settings.autoSave;
        SetNumTasksCompleted(_numTasksAssigned);

        // Apply player data after scene has loaded
        if (Player.Instance != null)
        {
            /*
            Vector3 position = new Vector3(playerData.playerPosition[0], playerData.playerPosition[1], playerData.playerPosition[2]);
            Player.Instance.transform.position = position;

            Transformation transformation = (Transformation)System.Enum.Parse(typeof(Transformation), playerData.playerForm);
            Player.Instance.SetTransformation(transformation);
            */
        }
        /*
        // Apply settings
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetGlobalVolume(playerData.settings.masterVolume);
        }

        Screen.SetResolution(playerData.settings.resolutionWidth, playerData.settings.resolutionHeight, playerData.settings.isFullscreen);
        */
    }
}
