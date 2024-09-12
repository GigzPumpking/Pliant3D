using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string levelSceneName;

    public void PlayGame()
    {
        EventDispatcher.Raise<PlayGame>(new PlayGame());
        SceneLoader.Instance.LoadNextScene(levelSceneName);
    }

    public void QuitGame()
    {
        EventDispatcher.Raise<QuitGame>(new QuitGame());
        SceneLoader.Instance.QuitFade();
    }
}
