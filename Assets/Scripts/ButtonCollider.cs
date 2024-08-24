using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonCollider : MonoBehaviour
{
    public ButtonScript button;

    private void OnTriggerEnter(Collider other)
    {
        button.PressButton();
        Debug.Log("Button pressed");
    }

    private void OnTriggerExit(Collider other)
    {
        button.UnpressButton();
        Debug.Log("Button unpressed");
    }
}
