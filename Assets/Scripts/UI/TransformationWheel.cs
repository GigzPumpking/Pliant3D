using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;

public class TransformationWheel : KeyActionReceiver
{
    public Vector2 normalisedMousePosition;
    public float currentAngle;
    public int hoveredSelection;
    [SerializeField] private int previousHover;
    //private int numOfSelection = 4;
    
    [SerializeField] private GameObject transformWheel;
    [SerializeField] private Image lockoutBar;
    
    public GameObject[]  transformationItems; //0[BULLDOZER], 1[FROG], 2[BALL], 3[TERRY]
    public Image[] transformationFills; //0[BULLDOZER], 1[FROG], 2[BALL], 3[TERRY] PARRALLALE WITH transformationItems
    
    private TransformationItem transformation;
    private TransformationItem previousTransformation;
    
    private GameObject smoke;
    private Animator smokeAnimator;

    [Header("Lockout Settings")]
    public float maxLockoutCharge = 100f; //default amount of max charge the player has for transforms
    public Dictionary<Transformation, float> LockoutProgresses = new Dictionary<Transformation, float>(); //the amount of "charge" the player has for each of the transforms
    public float transformCost = 25f; //the amount of "charge" it takes to transform

    [SerializeField] private bool _dbug = false;

    [SerializeField] private AudioData transformationSound;

    public void InitializeActionMap()
    {
        actionMap = new Dictionary<string, Action<InputAction.CallbackContext>>()
        {
            { "Ball", ctx => controllerSelect(0) },
            { "Frog", ctx => controllerSelect(1) },
            { "Bulldozer", ctx => controllerSelect(2) },
            { "Terry", ctx => controllerSelect(3) },
            { "Confirm", ctx => { if (ctx.performed) Transform(); } }
        };

        foreach (var action in actionMap.Keys)
        {
            InputManager.Instance.AddKeyBind(this, action, "Gameplay");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        smoke = Player.Instance.transform.Find("Smoke").gameObject;
        smokeAnimator = smoke.GetComponent<Animator>();

        SetWheel();
    }

    // Update is called once per frame
    void Update()
    {
        MouseHandler();
        InputHandler();
    }

    private void MouseHandler()
    {
        if (!(InputManager.Instance.ActiveDeviceType == "Keyboard") && !(InputManager.Instance.ActiveDeviceType == "Mouse")) 
        {
            return;
        }
        
        //Radial unit circle based off of screen and mouse position
        normalisedMousePosition = new Vector2(Input.mousePosition.x - Screen.width/2, 
            Input.mousePosition.y - Screen.height/2);
        
        // Because wheel is rotated add 45 to offset angle. Remove 45 at end if changed
        currentAngle = Mathf.Atan2(normalisedMousePosition.y, 
            normalisedMousePosition.x) * Mathf.Rad2Deg + 45;
        
        //bind angle to between 0 and 360 and clamp range between 0 and 359 to 
        currentAngle = Mathf.Clamp((currentAngle + 360)%360, 0, 359);
        
        //create index based off section of wheel over the number of selections
        hoveredSelection = (int)currentAngle/(360/transformationItems.Length);

        if (hoveredSelection != previousHover)
        {
            hoverSelect();
        }
    }

    private void InputHandler()
    {
        if (InputManager.Instance.IsInputEnabled) {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Transform();
        }
    }

    private void Transform()
    {
        if (!transformWheel.activeSelf) {
            return;
        }

        if (transformation == null)
        {
            Debug.LogWarning("No transformation selected");
            return;
        }

        var form = transformation.GetForm();
        // Commented out Lockout functionality:
        // if no charge left, and you're not trying to turn into terry, you can't transform
        // if (LockoutProgresses[transformation.GetForm().transformation] <= 0 &&
        //     form.transformation != Transformation.TERRY) return;

        // Play a random transformation sound from the list
        AudioManager.Instance?.PlayOneShot(transformationSound);

        Player.Instance.SetTransformation(form.transformation);
        transformWheel.SetActive(false);

        // Commented out subtracting lockout progress:
        // now, check that you're not substracting from the same transformation
        // if (previousTransformation != transformation) SubtractProgress(form.transformation, transformCost);

        Debug.Log("Current: " + transformation.GetForm().transformation + " Previous: " + previousTransformation.GetForm().transformation);
        
        EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = true });
    }

    private void hoverSelect() {
        //activate previous hover's animation
        previousTransformation = transformationItems[previousHover].GetComponent<TransformationItem>();
        previousTransformation.HoverExit();
        previousHover = hoveredSelection;
        
        //activate current hover's animaton.
        transformation = transformationItems[hoveredSelection].GetComponent<TransformationItem>();
        transformation.HoverEnter();
    }

    private void controllerSelect(int selection) {
        hoverSelect();
        hoveredSelection = selection;
    }

    int GetIntTransform()
    {
        Transformation current = transformation.GetForm().transformation;
        // 0[BULLDOZER], 1[FROG], 2[BALL], 3[TERRY]
        if (current == Transformation.BULLDOZER) return 0;
        else if (current == Transformation.FROG) return 1;
        else if (current == Transformation.BALL) return 2;
        else if (current == Transformation.TERRY) return 3;
        else return 3;
    }

    void SubtractProgress(Transformation t, float amt)
    {
        // Commented out Lockout functionality:
        // if (t == Transformation.TERRY)
        // {
        //     lockoutBar.fillAmount = 100;
        //     return;
        // }
        
        // Debug.Log("Subtracting from lockout: " + amt + " Current Lockout Charge for " + t + " : " + LockoutProgresses[t]);
        // LockoutProgresses[t] -= amt;
        // lockoutBar.fillAmount = LockoutProgresses[t] / 100;
        // transformationFills[GetIntTransform()].fillAmount = LockoutProgresses[t] / 100;
        // if (LockoutProgresses[t] <= 0f) Locked();
    }

    void AddProgress(Transformation t, float amt)
    {
        // Commented out Lockout functionality:
        // if (t == Transformation.TERRY)
        // {
        //     lockoutBar.fillAmount = 100;
        //     return;
        // }

        // Debug.Log("Adding to lockout: " + amt + " Current Lockout Charge for " + t + " : " + LockoutProgresses[t]);
        // LockoutProgresses[t] += amt;
        // lockoutBar.fillAmount = LockoutProgresses[t] / 100;
        // transformationFills[(int)t].fillAmount = LockoutProgresses[t] / 100;
        // if (LockoutProgresses[t] <= maxLockoutCharge) LockoutProgresses[t] = maxLockoutCharge;
    }

    public void ResetProgress()
    {
        // Commented out Lockout functionality:
        // foreach (var key in LockoutProgresses.Keys)
        // {
        //     LockoutProgresses[key] = maxLockoutCharge;
        // }
        // lockoutBar.fillAmount = 1;
    }

    void SetWheel()
    {
        // Commented out Lockout functionality:
        // LockoutProgresses[Transformation.FROG] = maxLockoutCharge;
        // LockoutProgresses[Transformation.TERRY] = maxLockoutCharge;
        // LockoutProgresses[Transformation.BULLDOZER] = maxLockoutCharge;
        // LockoutProgresses[Transformation.BALL] = maxLockoutCharge;
    }

    void Locked()
    {
        // Commented out Lockout functionality:
        // handle any extra functionalities here
        // Debug.LogWarning("Locked Out!");
    }
}
