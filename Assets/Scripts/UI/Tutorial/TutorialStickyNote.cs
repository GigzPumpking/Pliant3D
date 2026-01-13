using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialStickyNote : MonoBehaviour
{
    public Image StickyNoteImage;
    public Image StickyNoteGraphicHolder;
    public Sprite StickyNoteGraphicKeyboard;
    public Sprite StickyNoteGraphicController;
    public Image CompletionStamp;
    private Image _currGraphic;
    
    private void Update()
    {
        if (InputSystem.GetDevice<InputDevice>() is Gamepad && StickyNoteGraphicController) StickyNoteGraphicHolder.sprite = StickyNoteGraphicController;
        else if (InputSystem.GetDevice<InputDevice>() is Mouse or Keyboard && StickyNoteGraphicKeyboard) StickyNoteGraphicHolder.sprite = StickyNoteGraphicKeyboard;
    }

    private void Start()
    {
        OnHide();
    }

    public IEnumerator CompleteTask()
    {
        CompletionStamp?.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        OnHide();
    }
    
    public void OnShow()
    {
        if(StickyNoteImage) StickyNoteImage.enabled = true;
        StickyNoteGraphicHolder.enabled = true;
        
        gameObject.SetActive(true);
    }

    public void OnHide()
    {
        if(StickyNoteImage)StickyNoteImage.enabled = false;
        StickyNoteGraphicHolder.enabled = false;
        CompletionStamp?.gameObject.SetActive(false);
        
        gameObject.SetActive(false);
    }
}
