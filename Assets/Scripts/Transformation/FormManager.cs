using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // to make use of UI image class and sprite and make use of UI through code.
using UnityEngine.InputSystem; // to make use of input system to get input from player

/// <summary>
/// This form manager script is used to navigate the transformation menu to the characters forms
/// can easily be managed in the future whether forms are added and reduced.
/// 
/// -At start the the previous saved form will be selected otherwise start at the first form
/// 
/// -Next/Prev choic will cycle through the forms saved the database from front to back or vice versa
/// updating the form after called
/// 
/// -UpdateForm will change the selected form for quick transformation calling and future use.
/// </summary>
/// 

public class FormManager : MonoBehaviour
{
    [SerializeField] CharacterForm characterForm; //database of the forms the character will cycle through in the menu
    [SerializeField] SpriteRenderer formSprite; //the actual sprite for the given form
    [SerializeField] GameObject thoughtBubble;
    private GameObject smoke;
    private Animator smokeAnimator;

    public Image imageSprite; //the icon for the transformation menu icon
    public Image nextSprite; //the icon for the transformation menu icon of next to select
    public Image prevSprite; //the icon for the transformation menu icon of previous select

    private int selectedForm = 0, nextForm = 0, prevForm = 0; //indexes for the selected, next, and previous forms
    [SerializeField] public Image[] formImages; //array of the form images to be used in the transformation menu

    public PlayerControls playerControls; //player controls to get input from player

    private InputAction _transform;

    void Awake()
    {
        playerControls = new PlayerControls();
    }

    void OnEnable()
    {
        _transform = playerControls.Player.Transform;
        _transform.performed += ctx => SelectChoice();
        _transform.Enable();
    }

    void OnDisable()
    {
        _transform.performed -= ctx => SelectChoice();
        _transform.Disable();
    }

    // Start is called before the first frame update
    void Start()
    {
        smoke = Player.Instance.transform.Find("Smoke").gameObject;
        smokeAnimator = smoke.GetComponent<Animator>();
        // If the player had a previous selected previous load that form otherwise restart at the first form.
        if (!PlayerPrefs.HasKey("selectedForm"))
        {
            selectedForm = 0;
            prevForm = characterForm.formCount - 1;
            nextForm = selectedForm + 1;
        }
        else
            Load();

        UpdateForm(selectedForm, nextForm, prevForm);

        thoughtBubble.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            PrevChoice();

        if (Input.GetKeyDown(KeyCode.RightArrow))
            NextChoice();
    }

    //When called will cycle to the next form in the order unless at the end of the form database in which case it loops around to start
    public void NextChoice()
    {
        selectedForm++;

        if (selectedForm >= characterForm.formCount)
            selectedForm = 0;

        prevForm = selectedForm - 1;

        if (prevForm < 0)
            prevForm = characterForm.formCount - 1;

        nextForm = selectedForm + 1;

        if (nextForm >= characterForm.formCount)
            nextForm = 0;

        UpdateForm(selectedForm, nextForm, prevForm);
        Save();
    }

    //When called will cycle to the previous form in the order unless at the first of the form database in which case it loops around to end
    public void PrevChoice()
    {
        selectedForm--;

        if (selectedForm < 0)
            selectedForm = characterForm.formCount - 1;

        prevForm = selectedForm-1;

        if (prevForm < 0)
            prevForm = characterForm.formCount - 1;

        nextForm = selectedForm + 1;

        if (nextForm >= characterForm.formCount)
            nextForm = 0;

        UpdateForm(selectedForm, nextForm, prevForm);
        Save();
    }

    // Update the current form icon and sprite to be used when finished selecting from the menu when called
    private void UpdateForm(int selectedForm, int nextForm, int prevForm)
    {
        Form form = characterForm.GetForm(selectedForm);
        Form nform = characterForm.GetForm(nextForm);
        Form pform = characterForm.GetForm(prevForm);

        imageSprite.sprite = form.imageSprite;
        nextSprite.sprite = nform.imageSprite;
        prevSprite.sprite = pform.imageSprite;
    }

    //Load currently saved form data from when last saved in player's pref when called
    private void Load()
    {
        selectedForm = PlayerPrefs.GetInt("selectedForm");
        nextForm = PlayerPrefs.GetInt("nextForm");
        prevForm = PlayerPrefs.GetInt("prevForm");
    }

    //Save current selected form data when called for loading in future
    private void Save()
    {
        PlayerPrefs.SetInt("selectedForm", selectedForm);
        PlayerPrefs.SetInt("nextForm", nextForm);
        PlayerPrefs.SetInt("prevForm", prevForm);
    }

    public void SelectChoice()
    {
        // Set get form variable based off current form index
        Form form = characterForm.GetForm(selectedForm);

        //Set transformation corresponding to form information.

        Player.Instance.SetTransformation(form.transformation);

        // close thought bubble after selection.
        thoughtBubble.SetActive(false);

        EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = true });
    }
}
