using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI textDisplay;
    public string[] sentences;
    public float textSpeed;
    private int index;
    private bool active = false;

    [SerializeField] private Animator animator;

    void Awake()
    {
        textDisplay.text = string.Empty;
        index = 0;
        animator.Play("DialogueHide_Idle");
        active = false;
    }

    // Update is called once per frame
    void Update()
    {
        // if E is pressed, check if the text is done typing
        if (Input.GetKeyDown(KeyCode.E) && isActive()) 
        {
            checkNext();
        }
    }

    void StartDialogue() 
    {
        active = true;
        index = 0;
        StartCoroutine(TypeLine());
    }

    void NextLine() 
    {
        if (index < sentences.Length - 1) 
        {
            index++;
            textDisplay.text = string.Empty;
            StartCoroutine(TypeLine());
        }
        else 
        {
            textDisplay.text = "";
            animator.Play("DialogueHide");
            EventDispatcher.Raise<EndDialogue>(new EndDialogue());
            active = false;
        }
    }

    IEnumerator TypeLine() 
    {
        foreach (char letter in sentences[index].ToCharArray()) 
        {
            textDisplay.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    public void setSentences(string[] sentences) 
    {
        this.sentences = sentences;
    }

    public bool isActive() 
    {
        return active;
    }

    public void Appear() 
    {
        if (!validSentences()) 
        {
            return;
        }

        // Play Dialogue Appear animation
        animator.Play("DialogueAppear");
        textDisplay.text = string.Empty;
        // Start Dialogue after 1 second
        Invoke("StartDialogue", 1.0f);
    }

    public bool validSentences() 
    {
        return !(sentences == null || sentences.Length == 0);
    }

    public void checkNext() 
    {
        if (textDisplay.text == sentences[index]) 
        {
            NextLine();
        } else {
            StopAllCoroutines();
            textDisplay.text = sentences[index];
        }
    }
}
