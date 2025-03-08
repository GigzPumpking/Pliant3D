using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class DialogueEntry
{
    [TextArea]
    public string defaultText;
    [TextArea]
    public string keyboardText;
    [TextArea]
    public string controllerText;
}

public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI textDisplay;
    [SerializeField] TextMeshProUGUI continueButton;
    public DialogueEntry[] dialogueEntries;
    public float textSpeed;
    private int index;
    private bool active = false;
    
    // New property that updates UIManager when changed.
    private bool Active
    {
        get { return active; }
        set
        {
            active = value;
            if (UIManager.Instance != null)
                UIManager.Instance.isDialogueActive = value;
        }
    }

    [SerializeField] private Animator animator;
    string kbText = "Press 'E' to continue";
    string controllerText = "Press 'Y' to continue";

    void OnEnable()
    {
        EventDispatcher.AddListener<Interact>(PlayerInteract);
    }

    void OnDisable()
    {
        EventDispatcher.RemoveListener<Interact>(PlayerInteract);
    }

    void Awake()
    {
        textDisplay.text = string.Empty;
        index = 0;
        animator.Play("DialogueHide_Idle");
        Active = false;
    }

    void PlayerInteract(Interact e)
    {
        if (Active)
        {
            CheckNext();
        }
    }

    // Starts the dialogue. Note that any InputActionAsset references have been removed.
    void StartDialogue()
    {
        // Optionally disable player movement here.
        Player.Instance.canMoveToggle(false);

        Active = true;
        index = 0;
        StartCoroutine(TypeLine());
    }

    void NextLine()
    {
        if (index < dialogueEntries.Length - 1)
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
            EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = true });
            Active = false;
        }
    }

    IEnumerator TypeLine()
    {
        string sentence = GetCurrentSentence();
        foreach (char letter in sentence.ToCharArray())
        {
            textDisplay.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    public void SetDialogueEntries(DialogueEntry[] entries)
    {
        this.dialogueEntries = entries;
    }

    public bool IsActive()
    {
        return Active;
    }

    public void Appear()
    {
        if (!ValidEntries())
            return;

        animator.Play("DialogueAppear");
        textDisplay.text = string.Empty;
        // Start the dialogue after 1 second.
        Invoke("StartDialogue", 1.0f);
    }

    public bool ValidEntries()
    {
        return !(dialogueEntries == null || dialogueEntries.Length == 0);
    }

    public void CheckNext()
    {
        if (textDisplay.text == GetCurrentSentence())
        {
            NextLine();
        }
        else
        {
            StopAllCoroutines();
            textDisplay.text = GetCurrentSentence();
        }
    }

    // Returns the sentence text for the current index based on the active device.
    private string GetCurrentSentence()
    {
        if (dialogueEntries == null || dialogueEntries.Length <= index)
            return "";

        DialogueEntry entry = dialogueEntries[index];
        string activeDevice = InputManager.Instance?.ActiveDeviceType;
        if ((activeDevice == "Keyboard" || activeDevice == "Mouse") && !string.IsNullOrEmpty(entry.keyboardText))
        {
            return entry.keyboardText;
        }
        else if (!string.IsNullOrEmpty(entry.controllerText) && activeDevice != "Keyboard" && activeDevice != "Mouse")
        {
            return entry.controllerText;
        }
        return entry.defaultText;
    }

    void Update()
    {
        // Update the continue button text based on the current active device.
        if (InputManager.Instance?.ActiveDeviceType == "Keyboard" || InputManager.Instance?.ActiveDeviceType == "Mouse")
            continueButton.text = kbText;
        else
            continueButton.text = controllerText;
    }
}
