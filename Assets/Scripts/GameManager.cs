using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }

    public Transform player;

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
        if (Input.GetKeyDown(KeyCode.R)) {
            // Restart the game
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            Player.Instance.SetTransformation(Transformation.TERRY);
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
