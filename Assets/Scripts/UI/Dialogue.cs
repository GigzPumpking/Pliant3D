using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;

[System.Serializable]
public class DialogueEntry
{
    [TextArea(2, 5)]
    public string defaultText;
    
    [Tooltip("Enable to show separate keyboard/controller text fields for this entry")]
    public bool hasDeviceSpecificText;
    
    [TextArea(2, 5)]
    public string keyboardText;
    [TextArea(2, 5)]
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
    [SerializeField] string kbText = "Press 'E' to continue";
    [SerializeField] string controllerText = "Press 'Y' to continue";

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
        textDisplay.maxVisibleCharacters = 0;
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
            textDisplay.maxVisibleCharacters = 0;
            StartCoroutine(TypeLine());
        }
        else
        {
            textDisplay.text = "";
            textDisplay.maxVisibleCharacters = 0;
            animator.Play("DialogueHide");
            EventDispatcher.Raise<EndDialogue>(new EndDialogue(this.dialogueEntries[0].defaultText));
            EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = true });
            Active = false;
        }
    }

    IEnumerator TypeLine()
    {
        string sentence = GetCurrentSentenceStyled();
        textDisplay.text = sentence;
        textDisplay.maxVisibleCharacters = 0;
        textDisplay.ForceMeshUpdate();

        int totalChars = textDisplay.textInfo.characterCount;
        for (int i = 0; i < totalChars; i++)
        {
            textDisplay.maxVisibleCharacters = i + 1;
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
        textDisplay.maxVisibleCharacters = 0;
        // Start the dialogue after 1 second.
        Invoke("StartDialogue", 1.0f);
    }

    public bool ValidEntries()
    {
        return !(dialogueEntries == null || dialogueEntries.Length == 0);
    }

    public void CheckNext()
    {
        if (IsLineFullyRevealed())
        {
            NextLine();
        }
        else
        {
            StopAllCoroutines();
            textDisplay.ForceMeshUpdate();
            textDisplay.maxVisibleCharacters = textDisplay.textInfo.characterCount;
        }
    }

    // Returns the sentence text for the current index based on the active device.
    private string GetCurrentSentenceRaw()
    {
        if (dialogueEntries == null || dialogueEntries.Length <= index)
            return "";

        DialogueEntry entry = dialogueEntries[index];
        
        // Only check device-specific text if the entry has it enabled
        if (entry.hasDeviceSpecificText)
        {
            string activeDevice = InputManager.Instance?.ActiveDeviceType;
            if ((activeDevice == "Keyboard" || activeDevice == "Mouse") && !string.IsNullOrEmpty(entry.keyboardText))
            {
                return entry.keyboardText;
            }
            else if (!string.IsNullOrEmpty(entry.controllerText) && activeDevice != "Keyboard" && activeDevice != "Mouse")
            {
                return entry.controllerText;
            }
        }
        
        return entry.defaultText;
    }

    private string GetCurrentSentenceStyled()
    {
        return ApplyDialogueStyling(GetCurrentSentenceRaw());
    }

    private bool IsLineFullyRevealed()
    {
        textDisplay.ForceMeshUpdate();
        int totalChars = textDisplay.textInfo.characterCount;
        return totalChars == 0 || textDisplay.maxVisibleCharacters >= totalChars;
    }

    private string ApplyDialogueStyling(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Convert **text** to bold using TMP rich text.
        string output = Regex.Replace(input, @"\*\*(.+?)\*\*", "<b>$1</b>");

        // Convert *text* to italics (single asterisks only).
        output = Regex.Replace(output, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "<i>$1</i>");

        // Convert ~~text~~ to strikethrough.
        output = Regex.Replace(output, @"~~(.+?)~~", "<s>$1</s>");

        // Convert [color=#RRGGBB]text[/color] to TMP color tags.
        output = Regex.Replace(output, @"\[color=(#?[A-Za-z0-9]+)\](.+?)\[/color\]", "<color=$1>$2</color>");

        return output;
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
