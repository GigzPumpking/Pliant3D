using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewSceneChecker : MonoBehaviour
{
    // On Awake, raise event that a new scene has been loaded, then destroy this object
    private void Awake()
    {
        EventDispatcher.Raise<NewSceneLoaded>(new NewSceneLoaded() 
        { 
            sceneName = SceneManager.GetActiveScene().name 
        });
        Destroy(this.gameObject);
    }
}
