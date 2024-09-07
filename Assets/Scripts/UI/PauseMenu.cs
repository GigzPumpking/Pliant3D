using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    private SceneLoader sceneloader;

    public GameObject pauseMenu;

    public GameObject loader;

    public static bool isPaused;

    private void Awake()
    {
        sceneloader = loader.GetComponent<SceneLoader>();
    }

    // Start is called before the first frame update
    void Start()
    {
        pauseMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        FindAnyObjectByType<AudioManager>().Pause("Ambience");
    }

    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        FindAnyObjectByType<AudioManager>().Play("Crumple");
        FindAnyObjectByType<AudioManager>().Play("Ambience");
    }

    public void ReturnToMainMenu()
    {
        FindAnyObjectByType<AudioManager>().Play("Ambience");
        FindAnyObjectByType<AudioManager>().Stop("Ambience");
        FindAnyObjectByType<AudioManager>().Stop("Radio");

        Time.timeScale = 1f;

        sceneloader.LoadNextScene("Main Menu");
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        FindAnyObjectByType<AudioManager>().Play("Ambience");
        sceneloader.QuitFade();
    }

    public bool checkPause() 
    {
        return isPaused;
    }
}
