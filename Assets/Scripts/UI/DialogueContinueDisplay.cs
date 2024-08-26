using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueContinueDisplay : MonoBehaviour
{
    private Image image;

    public IsometricCharacterController playerScript;

    public Sprite keyboardContinue;
    public Sprite gamepadContinue;

    void Start() {
        image = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (playerScript.GetLastInputDevice() == 0) {
            image.sprite = keyboardContinue;
        }
        else {
            image.sprite = gamepadContinue;
        }
    }


}
