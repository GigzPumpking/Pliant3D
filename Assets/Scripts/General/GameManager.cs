using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Linq.Expressions;
using UnityEngine.EventSystems;

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

    // Main menu scene name
    [SerializeField] private string mainMenuSceneName = "0 Main Menu";

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
            //Handle Music Carryover between scenes
            instance.mainTheme = this.mainTheme;
            instance.Ambience  = this.Ambience;
            AudioManager.Instance?.DeleteCurrentMusicSources();
            AudioManager.Instance?.PlayMusic(mainTheme);
            AudioManager.Instance?.PlayMusic(Ambience);
            
            Destroy(this.gameObject);
            return;
        }
        DontDestroyOnLoad(this.gameObject);

        // Subscribe to new-scene events for auto-save
        EventDispatcher.AddListener<NewSceneLoaded>(OnNewSceneLoaded);
    }

    void Start()
    {
        AudioManager.Instance?.PlayMusic(mainTheme);
        AudioManager.Instance?.PlayMusic(Ambience);
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

    public void ToggleAutoSave()
    {
        AutoSaveEnabled = !AutoSaveEnabled;
    }

    public void SetAutoSave(bool enabled)
    {
        AutoSaveEnabled = enabled;
    }

    private void OnNewSceneLoaded(NewSceneLoaded e)
    {
        if (!AutoSaveEnabled) return;

        // Don't auto-save on unsaveable scenes (main menu, transitions, etc.)
        if (unsaveableScenes.Contains(e.sceneName)) return;

        SaveGame();
        Debug.Log("Auto-saved on scene load: " + e.sceneName);
    }

    private void OnDestroy()
    {
        EventDispatcher.RemoveListener<NewSceneLoaded>(OnNewSceneLoaded);
    }

    public void Quit()
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
        // Load the scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(playerData.sceneName);

        // Wait until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Restore auto-save preference
        AutoSaveEnabled = playerData.settings.autoSave;

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
