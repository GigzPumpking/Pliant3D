using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using InputDevice = UnityEngine.XR.InputDevice;

public class NewSceneChecker : MonoBehaviour
{
    public string KeyboardTXT = "Press 'E' to continue";
    public string ControllerTXT = "Press 'A' to continue";
        
    public TextMeshProUGUI transitionText; // Reference to the TextMeshProUGUI component to display the scene name
    // On Awake, raise event that a new scene has been loaded, then destroy this object
    
    private void Awake()
    {
        EventDispatcher.Raise<NewSceneLoaded>(new NewSceneLoaded() 
        { 
            sceneName = SceneManager.GetActiveScene().name 
        });
        //Destroy(this.gameObject);
        
        if (!transitionText) return;
        InputSystem.onDeviceChange += (device, change) =>
        {
            switch (device)
            {
                case Mouse or Keyboard:
                    transitionText.text = KeyboardTXT;
                    break;
                case Gamepad:
                    transitionText.text = ControllerTXT;
                    break;
            }
        };
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E) || Gamepad.current?.buttonSouth.wasPressedThisFrame == true)
        {
            CallLoadNextScene();
        }
    }
    
    public void CallLoadNextScene()
    {
        Debug.Log("Loading next scene: " + NextScene.TargetScene);
        UIManager.Instance.FadeOut();
        NextScene.CallLoadNextScene();
    }
}
