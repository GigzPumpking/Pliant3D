using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;

    public static UIManager Instance { get { return instance; } }
    private Dialogue dialogueScript;
    public GameObject sceneTransition;
    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject pauseButton;
    [SerializeField] GameObject resumeButton;

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

        if(!pauseMenu) pauseMenu = transform.Find("Pause Menu").gameObject;

        EventDispatcher.AddListener<NewSceneLoaded>(FadeOut);

        if(!resumeButton) resumeButton = GameObject.Find("Resume Button");
        if(!pauseButton)  pauseButton = GameObject.Find("Pause Button");
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            Pause();
        }
    }

    public void Pause() {
        // Pause the game
        // play Crumple Select sound
        EventDispatcher.Raise<PlaySound>(new PlaySound { soundName = "Crumple Select", source = null });

        //no null checks here since I want to know if there is something not being found
        if (pauseMenu.activeSelf) {
            pauseButton.SetActive(true);
            resumeButton.SetActive(false);
            pauseMenu.SetActive(false);
            Time.timeScale = 1;
        } else {
            pauseMenu.SetActive(true);
            pauseButton.SetActive(false);
            resumeButton.SetActive(true);
            Time.timeScale = 0;
        }
    }

    private void OnDestroy()
    {
        EventDispatcher.RemoveListener<NewSceneLoaded>(FadeOut);
    }
    
    public void ToggleButton(GameObject button)
    {
        if (button.GetComponent<Image>().color == Color.red)
            button.GetComponent<Image>().color = Color.green;
        else
            button.GetComponent<Image>().color = Color.red;
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
