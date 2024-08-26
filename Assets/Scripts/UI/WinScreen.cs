using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinScreen : MonoBehaviour
{
    // On wake up, remove player
    void Awake()
    {
        if (GameManager.Instance != null) {
            Destroy(GameManager.Instance.gameObject);
        }

        if (AudioManager.Instance != null) {
            Destroy(AudioManager.Instance.gameObject);
        }

        if (IsometricCharacterController.Instance != null) {
            Destroy(IsometricCharacterController.Instance.gameObject);
        }

        if (UIManager.Instance != null) {
            Destroy(UIManager.Instance.gameObject);
        }
    }

    public void Transition()
    {
        SceneLoader.Instance.LoadNextScene("Main Menu");
    }
}
