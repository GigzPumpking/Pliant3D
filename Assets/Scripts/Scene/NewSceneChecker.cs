using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;

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
        if(InputSystem.GetDevice<Keyboard>() != null || InputSystem.GetDevice<Mouse>() != null)
        {
            transitionText.text = KeyboardTXT;
        }
        else if(InputSystem.GetDevice<Gamepad>() != null)
        {
            transitionText.text = ControllerTXT;
        }
    }

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (!transitionText) return;
        switch (device)
        {
            case Mouse or Keyboard or Pointer:
                transitionText.text = KeyboardTXT;
                break;
            case Gamepad or Joystick:
                transitionText.text = ControllerTXT;
                break;
        }
    }

    bool hasntBeenCalled = true;
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E) || Gamepad.current?.buttonSouth.wasPressedThisFrame == true && hasntBeenCalled)
        {
            CallLoadNextScene();
            hasntBeenCalled = false;
        }

        if (!transitionText) return;
        if (InputSystem.GetDevice<InputDevice>() is Gamepad) transitionText.text = ControllerTXT;
        else if (InputSystem.GetDevice<InputDevice>() is Mouse or Keyboard) transitionText.text = KeyboardTXT;
    }
    
    public void CallLoadNextScene()
    {
        Debug.Log("Loading next scene: " + NextScene.TargetScene);
        UIManager.Instance?.FadeOut();
        NextScene.CallLoadNextScene();
    }

    public void LoadMainMenu()
    {
        UIManager.Instance?.FadeOut();
        SceneManager.LoadScene("0 Main Menu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
