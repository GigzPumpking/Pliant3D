using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuAudio : MonoBehaviour
{
    private static MainMenuAudio instance;
    private AudioSource audioSource;

    private void Start()
    {

        if (instance == null)
            instance = this;
        else
        {
            Destroy(this.gameObject);
            return;
        }
        DontDestroyOnLoad(this.gameObject);

        audioSource = GetComponent<AudioSource>();
        EventDispatcher.AddListener<PlayGame>(PlayGame);
        EventDispatcher.AddListener<QuitGame>(QuitGame);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        gameObject.SetActive(scene.name == "0 Main Menu");
    }

    private void OnDestroy()
    {
        EventDispatcher.RemoveListener<PlayGame>(PlayGame);
        EventDispatcher.RemoveListener<QuitGame>(QuitGame);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void PlayGame(PlayGame e)
    {
        StopBackground();
        //PlaySound("Main Menu Play", audioSource);
    }

    public void QuitGame(QuitGame e)
    {
        CrumplePlay();
    }

    public void PlayBackground()
    {
        PlayMusic("Main Menu BGM");
        PlayMusic("Main Menu Ambience");
    }

    public void StopBackground()
    {
        StopMusic("Main Menu BGM");
        StopMusic("Main Menu Ambience");
    }

    public void DingPlay()
    {
        PlaySound("Ding Select", audioSource);
    }

    public void CrumplePlay()
    {
        PlaySound("Crumple Select", audioSource);
    }

    private void PlaySound(string soundName, AudioSource source)
    {
        EventDispatcher.Raise<PlaySound>(new PlaySound() {
            soundName = soundName,
            source = source
        });
    }

    private void PlayMusic(string musicName)
    {
        EventDispatcher.Raise<PlayMusic>(new PlayMusic() {
            musicName = musicName
        });
    }

    private void StopMusic(string musicName)
    {
        EventDispatcher.Raise<StopMusic>(new StopMusic() {
            musicName = musicName
        });
    }
}
