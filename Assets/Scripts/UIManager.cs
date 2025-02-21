using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;

    public static UIManager Instance { get { return instance; } }

    private Dialogue dialogueScript;

    public GameObject sceneTransition;

    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject pauseMain;
    [SerializeField] private GameObject controls;
    [SerializeField] private GameObject settings;
    [SerializeField] private GameObject pauseButton;
    
    private TextMeshProUGUI pauseButtonText;

    [SerializeField] private string pauseButtonTextKb = "PAUSE (ESC)";
    [SerializeField] private string pauseButtonTextController = "PAUSE (START)";
    [SerializeField] private GameObject resumeButton;

    private AudioPlayer audioPlayer;

    public bool isPaused {
        get {
            return pauseMenu.activeSelf;
        }
    }

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

        audioPlayer = GetComponent<AudioPlayer>();

        if(!pauseMenu) pauseMenu = transform.Find("Pause Menu").gameObject;
        pauseMain = pauseMenu.transform.Find("Pause Main").gameObject;
        controls = pauseMenu.transform.Find("Controls").gameObject;
        settings = pauseMenu.transform.Find("Settings").gameObject;
        if(!pauseButton) pauseButton = transform.Find("Pause Button").gameObject;
        pauseButtonText = pauseButton.GetComponentInChildren<TextMeshProUGUI>();
        if(!resumeButton) resumeButton = pauseMenu.transform.Find("Resume Button").gameObject;

        pauseMenu.SetActive(false);
        pauseButton.SetActive(true);

        EventDispatcher.AddListener<NewSceneLoaded>(FadeOut);
    }

    void Update() {
        if (InputManager.Instance?.ActiveDeviceType == "Keyboard" || InputManager.Instance?.ActiveDeviceType == "Mouse") {
            pauseButtonText.text = pauseButtonTextKb;
        } else {
            pauseButtonText.text = pauseButtonTextController;
        }
    }

    public void Pause() {
        // Pause the game
        // play Crumple Select sound
        audioPlayer?.PlayOneShot("Crumple Select");

        //no null checks here since I want to know if there is something not being found
        if (pauseMenu.activeSelf) {
            pauseButton?.SetActive(true);
            resumeButton?.SetActive(false);
            pauseMenu?.SetActive(false);
            Time.timeScale = 1;
        } else {
            pauseMenu?.SetActive(true);
            pauseMain?.SetActive(true);
            controls?.SetActive(false);
            settings?.SetActive(false);
            pauseButton?.SetActive(false);
            resumeButton?.SetActive(true);
            Time.timeScale = 0;
        }
    }

    public void Quit() {
        if (GameManager.Instance != null) {
            GameManager.Instance.Quit();
        } else {
            Application.Quit();
        }
    }

    private void OnDestroy()
    {
        EventDispatcher.RemoveListener<NewSceneLoaded>(FadeOut);
    }

    public void FadeOut()
    {
        sceneTransition.GetComponent<Animator>().SetTrigger("FadeOut");
    }

    public void FadeIn()
    {
        sceneTransition.SetActive(true);
        sceneTransition.GetComponent<Animator>().SetTrigger("FadeIn");
    }

    public void FadeOut(NewSceneLoaded e)
    {
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
}
