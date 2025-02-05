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
    private GameObject pauseMenu;

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

        EventDispatcher.AddListener<NewSceneLoaded>(FadeOut);
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
        if (pauseMenu.activeSelf) {
            pauseMenu.SetActive(false);
            Time.timeScale = 1;
        } else {
            pauseMenu.SetActive(true);
            Time.timeScale = 0;
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
