using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }
    public Transform player;
    public GameObject pauseMenu;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }

        DontDestroyOnLoad(this.gameObject);
    }

    private void Update() {
        // Backspace to restart the game
        if (Input.GetKeyDown(KeyCode.Backspace)) {
            // Restart the game
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            Player.Instance.SetTransformation(Transformation.TERRY);
            // set Player velocity to 0
            Player.Instance.SetVelocity(Vector3.zero);
        }

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

    public void SetPlayer(Transform player)
    {
        this.player = player;
    }

    public Transform GetPlayer()
    {
        return player;
    }

}
