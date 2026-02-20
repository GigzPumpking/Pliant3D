using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Linq.Expressions;

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
            AudioManager.Instance.DeleteCurrentMusicSources();
            AudioManager.Instance.PlayMusic(mainTheme);
            AudioManager.Instance.PlayMusic(Ambience);
            
            Destroy(this.gameObject);
            return;
        }
        DontDestroyOnLoad(this.gameObject);
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
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Debug.Log("Game Over");
        Player.Instance.canMoveToggle(false);
    }

    public void Reset(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Reset();
        }
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

    public void SaveGame(string saveFileName)
    {
        PlayerData playerData = new PlayerData();

        if (Player.Instance != null)
        {
            playerData.sceneName = SceneManager.GetActiveScene().name;
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

        SaveSystem.SaveGame(saveFileName, playerData);
    }

    public void LoadGame(string saveFileName)
    {
        PlayerData playerData = SaveSystem.LoadGame(saveFileName);
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
