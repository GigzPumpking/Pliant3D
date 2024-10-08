using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;

    public static UIManager Instance { get { return instance; } }

    [SerializeField] private Dialogue dialogueScript;

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
    }
    
    public void ToggleButton(GameObject button)
    {
        if (button.GetComponent<Image>().color == Color.red)
            button.GetComponent<Image>().color = Color.green;
        else
            button.GetComponent<Image>().color = Color.red;
    }

    public Dialogue returnDialogue()
    {
        return dialogueScript;
    }
}
