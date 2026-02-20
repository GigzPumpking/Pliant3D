using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : KeyActionReceiver<UIManager>
{
    private static UIManager instance;
    public static UIManager Instance { get { return instance; } }

    private Dialogue dialogueScript;

    public bool isDialogueActive = false;
    public GameObject sceneTransition;
    public UILoadingScreen loadingScreen;
    public DayBanner dayBanner;

    public GameObject scenePanelPrefab;

    private GameObject pauseMenu;
    private GameObject pauseMain;
    private GameObject controls;
    private GameObject settings;
    private GameObject pauseButton;
    
    private TextMeshProUGUI pauseButtonText;

    [SerializeField] private string pauseButtonTextKb = "PAUSE (ESC)";
    [SerializeField] private string pauseButtonTextController = "PAUSE (START)";
    private GameObject resumeButton;

    [SerializeField] private AudioData pauseSound;

    [SerializeField] private List<string> scenesToHidePauseIn = new List<string>();

    [SerializeField] private GameObject GameOverPanel;

    public bool isPaused
    {
        get
        {
            return pauseMenu.activeSelf;
        }
    }

    // Serialized list of back button GameObjects representing different menus.
    [SerializeField] private List<GameObject> backButtons = new List<GameObject>();

    // Static key mapping shared across all UIManager instances.
    public static Dictionary<string, Action<UIManager, InputAction.CallbackContext>> staticKeyMapping =
        new Dictionary<string, Action<UIManager, InputAction.CallbackContext>>()
        {
            { "Cancel", (instance, ctx) => instance.Cancel(ctx) }
        };

    protected override Dictionary<string, Action<UIManager, InputAction.CallbackContext>> KeyMapping => staticKeyMapping;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }

        DontDestroyOnLoad(this);

        dialogueScript = transform.Find("DialogueBox").GetComponent<Dialogue>();

        pauseMenu = transform.Find("Pause Menu").gameObject;
        pauseMain = pauseMenu.transform.Find("Pause Main").gameObject;
        controls = pauseMenu.transform.Find("Controls").gameObject;
        settings = pauseMenu.transform.Find("Settings").gameObject;
        pauseButton = transform.Find("Pause Button").gameObject;
        pauseButtonText = pauseButton.GetComponentInChildren<TextMeshProUGUI>();
        resumeButton = pauseMenu.transform.Find("Resume Button").gameObject;

        pauseMenu.SetActive(false);
        UpdatePauseButtonVisibility();

        EventDispatcher.AddListener<NewSceneLoaded>(FadeOut);
        EventDispatcher.AddListener<NewSceneLoaded>(OnSceneChanged);
    }

    void Update()
    {
        /*
        if (InputManager.Instance?.ActiveDeviceType == "Keyboard" || InputManager.Instance?.ActiveDeviceType == "Mouse") {
            pauseButtonText.text = pauseButtonTextKb;
        } else {
            pauseButtonText.text = pauseButtonTextController;
        }
        */
    }

    public void CallGameManagerLevelReset()
    {
        if (!GameManager.Instance) return;
        GameManager.Instance.Reset();
    }

    private static BaseEventData eventSystemData;
    public void GameOverProtocol()
    {
        if (GameOverPanel != null)
            GameOverPanel.SetActive(true);
        
        var eventSystem = EventSystem.current;
        bool can = GameOverPanel.transform.GetChild(2).gameObject != null;
        
        if(can) 
            eventSystem.SetSelectedGameObject(GameOverPanel.transform.GetChild(2).gameObject, eventSystemData);

        Debug.Log("Game Over");
        TogglePlayerMovement tpm = new TogglePlayerMovement();
        tpm.isEnabled = false;
        EventDispatcher.Raise(tpm);
    }

    public void DisableGameOverPanel()
    {
        GameOverPanel.SetActive(false);
    }

    public void Pause()
    {
        // Pause the game
        AudioManager.Instance?.PlayOneShot(pauseSound);

        // no null checks here since I want to know if there is something not being found
        if (pauseMenu.activeSelf)
        {
            UpdatePauseButtonVisibility();
            resumeButton?.SetActive(false);
            pauseMenu?.SetActive(false);
            Time.timeScale = 1;
        }
        else
        {
            pauseMenu?.SetActive(true);
            pauseMain?.SetActive(true);
            controls?.SetActive(false);
            settings?.SetActive(false);
            pauseButton?.SetActive(false);
            resumeButton?.SetActive(true);
            Time.timeScale = 0;
        }
    }

    public void Quit()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Quit();
        }
        else
        {
            Application.Quit();
        }
    }

    private void OnDestroy()
    {
        EventDispatcher.RemoveListener<NewSceneLoaded>(FadeOut);
        EventDispatcher.RemoveListener<NewSceneLoaded>(OnSceneChanged);
    }

    private void OnSceneChanged(NewSceneLoaded e)
    {
        // When a new scene loads, update the button's visibility based on our list.
        // We only do this if the game isn't paused, as the Pause() method handles visibility during a paused state.
        if (!isPaused)
        {
            UpdatePauseButtonVisibility();
        }
    }

    private void UpdatePauseButtonVisibility()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        // If the current scene is in our list, hide the button. Otherwise, show it.
        bool shouldHide = scenesToHidePauseIn.Contains(currentSceneName);
        pauseButton.SetActive(!shouldHide);
    }

    public void FadeOut()
    {
        sceneTransition.GetComponent<Animator>().SetTrigger("FadeOut");
    }

    public void FadeIn()
    {
        loadingScreen.mainCamera = Camera.main;
        sceneTransition.SetActive(true);
        sceneTransition.GetComponent<Animator>().SetTrigger("FadeIn");
    }

    public void FadeOut(NewSceneLoaded e)
    {
        loadingScreen.mainCamera = Camera.main;
        FadeOut();
    }

    public Dialogue returnDialogue()
    {
        return dialogueScript;
    }

    public GameObject returnPauseMenu()
    {
        return pauseMenu;
    }

    public GameObject returnScenePanel()
    {
        return scenePanelPrefab;
    }

    // Overload to support InputAction.CallbackContext.
    public void Cancel(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Cancel();
        }
    }

    // This Cancel method loops through the backButtons list in reverse order.
    // When it finds the most recent button that is active in the hierarchy,
    // it invokes its onClick event.
    public void Cancel()
    {
        for (int i = backButtons.Count - 1; i >= 0; i--)
        {
            if (backButtons[i] != null && backButtons[i].activeInHierarchy)
            {
                Button backButtonComponent = backButtons[i].GetComponent<Button>();
                if (backButtonComponent != null)
                {
                    backButtonComponent.onClick.Invoke();
                    return;
                }
            }
        }
    }

    public void ResetLevel()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void SaveGame()
    {
        GameManager.Instance?.SaveGame();
    }

    public void LoadGame()
    {
        GameManager.Instance?.LoadGame();
    }

    /// <summary>
    /// Call from a UI Toggle to flip the auto-save setting.
    /// </summary>
    public void ToggleAutoSave()
    {
        GameManager.Instance?.ToggleAutoSave();
    }

    /// <summary>
    /// Call from a UI Toggle's onValueChanged to set auto-save on/off.
    /// </summary>
    public void SetAutoSave(bool enabled)
    {
        GameManager.Instance?.SetAutoSave(enabled);
    }

    /// <summary>
    /// Returns current auto-save state (for initializing a UI Toggle).
    /// </summary>
    public bool GetAutoSaveEnabled()
    {
        return GameManager.Instance != null && GameManager.Instance.AutoSaveEnabled;
    }
}
