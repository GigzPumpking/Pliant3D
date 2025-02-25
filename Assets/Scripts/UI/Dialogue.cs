using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI textDisplay;
    [SerializeField] TextMeshProUGUI continueButton;
    public string[] sentences;
    public float textSpeed;
    private int index;
    private bool active = false;
    
    // New property that updates UIManager when changed
    private bool Active {
        get { return active; }
        set {
            active = value;
            if (UIManager.Instance != null) UIManager.Instance.isDialogueActive = value;
        }
    }

    [SerializeField] InputActionAsset inputActionAsset;
    string kbText = "Press 'E' to continue";
    string controllerText = "Press 'Y' to continue";

    [SerializeField] private Animator animator;

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
            checkNext();
        }
    }

    void StartDialogue() 
    {
        inputActionAsset.Enable();
        Player.Instance.canMoveToggle(false);

        Active = true;  // Use the property so UIManager is updated
        index = 0;
        inputActionAsset.FindAction("Interact").performed += SetContinueText;

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
            EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = true });
            Active = false;  // Use the property so UIManager is updated
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
        return Active;
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
        } 
        else 
        {
            StopAllCoroutines();
            textDisplay.text = sentences[index];
        }
    }

    void SetContinueText(InputAction.CallbackContext ctx)
    {
        if (!Active) return;

        if(ctx.control.device is Keyboard || ctx.control.device is Mouse) 
            continueButton.text = kbText;
        else 
            continueButton.text = controllerText;
    }
}
