using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialStickyNote : MonoBehaviour
{
    private static TutorialStickyNote instance;
    public static TutorialStickyNote Instance { get { return instance; } }
    
    public Image StickyNoteImage;
    public Image StickyNoteGraphic;
    public Image CompletionStamp;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        OnHide();
    }

    public IEnumerator CompleteTask()
    {
        CompletionStamp.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        OnHide();
    }
    
    public void OnShow()
    {
        StickyNoteImage.enabled = true;
        StickyNoteGraphic.enabled = true;
    }

    public void OnHide()
    {
        StickyNoteImage.enabled = false;
        StickyNoteGraphic.enabled = false;
        CompletionStamp.gameObject.SetActive(false);
    }
}
