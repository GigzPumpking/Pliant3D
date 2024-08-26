using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string levelSceneName;

    private void Start()
    {
        AudioManager.Instance.Play("Main Menu BGM");
        AudioManager.Instance.Play("Main Menu Ambience");
    }

    public void PlayGame()
    {
        AudioManager.Instance.Stop("Main Menu BGM");
        AudioManager.Instance.Stop("Main Menu Ambience");
        AudioManager.Instance.Play("Main Menu Play");
        SceneLoader.Instance.LoadNextScene(levelSceneName);
    }

    public void PlayCrumpleSound()
    {
        AudioManager.Instance.Play("Crumple");
    }

    public void QuitGame()
    {
        PlayCrumpleSound();
        SceneLoader.Instance.QuitFade();
    }
}
