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

    void Start() 
    {
        gameObject.SetActive(false);
    }

    void Awake()
    {
        textDisplay.text = string.Empty;
        index = 0;
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
            gameObject.SetActive(false);
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
        return gameObject.activeSelf;
    }

    public void Appear() 
    {
        gameObject.SetActive(true);
        textDisplay.text = string.Empty;
        StartDialogue();
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
