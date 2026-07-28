using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance { get; private set; }
    [SerializeField] private string mainMenuSceneName = "0 Main Menu";
    [SerializeField] private string endScreenSceneName;

    [Header("Main Menu Objects")]
    [SerializeField] private List<GameObject> mainMenuObjects = new List<GameObject>();

    [Header("End Screen Objects")]
    [SerializeField] private List<GameObject> endScreenObjects = new List<GameObject>();

    [Header("Always Hidden on Show")]
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
        bool isEndScreen = scene.name == endScreenSceneName;

        gameObject.SetActive(isMainMenu || isEndScreen);

        if (isMainMenu || isEndScreen)
        {
            foreach (GameObject obj in mainMenuObjects)
                if (obj != null) obj.SetActive(isMainMenu);

            foreach (GameObject obj in endScreenObjects)
                if (obj != null) obj.SetActive(isEndScreen);

            foreach (GameObject obj in objectsToDisableOnShow)
                if (obj != null) obj.SetActive(false);
        }
    }
}
