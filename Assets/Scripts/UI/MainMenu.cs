using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance { get; private set; }

    [SerializeField] private string levelSceneName;
    [SerializeField] private string mainMenuSceneName = "0 Main Menu";
    [SerializeField] private List<GameObject> objectsToDisableOnShow = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isMainMenu = scene.name == mainMenuSceneName;
        gameObject.SetActive(isMainMenu);
        if (isMainMenu)
        {
            foreach (GameObject obj in objectsToDisableOnShow)
            {
                if (obj != null) obj.SetActive(false);
            }
        }
    }
}
