using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class NextSceneTrigger : MonoBehaviour {
    public static event Action NextSceneTriggered;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            UIManager.Instance.FadeIn();
            
            //listened to by 'ObjectiveTracker.cs'
            NextSceneTriggered?.Invoke();
        }
    }
}
