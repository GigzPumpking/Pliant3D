using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }
    [SerializeField] private Transform player;

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

    [SerializeField] TransformationWheel transformWheel;
    private void Update() {
        if (!transformWheel) transformWheel = GameObject.FindObjectOfType<TransformationWheel>();

        // Backspace to restart the game
        if (Input.GetKeyDown(KeyCode.Backspace)) {
            // Restart the game
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

            Player.Instance.SetTransformation(Transformation.TERRY);
            // set Player velocity to 0
            Player.Instance.SetVelocity(Vector3.zero);
            if (transformWheel != null) transformWheel.ResetProgress();
            else Debug.LogWarning("Could not reset lockout charge");
        }
    }

    public void SetPlayer(Transform player)
    {
        this.player = player;
    }

    public Transform GetPlayer()
    {
        if (player == null)
        {
            player = Player.Instance.transform;
        }

        return player;
    }

}
