using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }
    private Transform player;

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

            //Reset lockout charge
            Player.Instance.TryGetComponent<TransformationWheel>(out TransformationWheel transformWheel);
            transformWheel.lockoutProgress = transformWheel.maxLockoutCharge;

            Player.Instance.SetTransformation(Transformation.TERRY);
            // set Player velocity to 0
            Player.Instance.SetVelocity(Vector3.zero);
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
