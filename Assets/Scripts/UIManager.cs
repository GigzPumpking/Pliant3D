using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;

    public static UIManager Instance { get { return instance; } }
    private Dialogue dialogueScript;
    public GameObject sceneTransition;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }

        DontDestroyOnLoad(this);

        dialogueScript = transform.Find("DialogueBox").GetComponent<Dialogue>();

        EventDispatcher.AddListener<NewSceneLoaded>(FadeOut);
    }

    private void OnDestroy()
    {
        EventDispatcher.RemoveListener<NewSceneLoaded>(FadeOut);
    }
    
    public void ToggleButton(GameObject button)
    {
        if (button.GetComponent<Image>().color == Color.red)
            button.GetComponent<Image>().color = Color.green;
        else
            button.GetComponent<Image>().color = Color.red;
    }

    public void FadeOut()
    {
        sceneTransition.GetComponent<Animator>().SetTrigger("FadeOut");
    }

    public void FadeIn()
    {
        sceneTransition.SetActive(true);
        sceneTransition.GetComponent<Animator>().SetTrigger("FadeIn");
    }

    public void FadeOut(NewSceneLoaded e)
    {
        FadeOut();
    }

    public Dialogue returnDialogue()
    {
        return dialogueScript;
    }
}
