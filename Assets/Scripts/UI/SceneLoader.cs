using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private static SceneLoader instance;

    public static SceneLoader Instance { get { return instance; } }

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    public Animator transition;

    public float transitionTimer = 1f;

    public void LoadNextScene(string newScene)
    {
        StartCoroutine(LoadScene(newScene));
    }
    public void LoadNextScene(int newScene)
    {
        StartCoroutine(LoadScene(newScene));
    }

    public void QuitFade()
    {
        StartCoroutine(QuitTransition());
    }

    IEnumerator LoadScene(string newScene)
    {
        Debug.Log("Loading scene: " + newScene);
        transition.SetTrigger("Start");

        yield return new WaitForSeconds(transitionTimer);

        SceneManager.LoadScene(newScene);
    }

    IEnumerator LoadScene(int newScene)
    {
        Debug.Log("Loading scene: " + newScene);
        transition.SetTrigger("Start");

        yield return new WaitForSeconds(transitionTimer);

        SceneManager.LoadScene(newScene);
    }

    IEnumerator QuitTransition()
    {
        transition.SetTrigger("Start");

        yield return new WaitForSeconds(transitionTimer);

        Debug.Log("Application ended");
        Application.Quit();
    }

}
