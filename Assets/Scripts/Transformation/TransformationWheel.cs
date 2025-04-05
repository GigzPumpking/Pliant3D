using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;

public class TransformationWheel : KeyActionReceiver<TransformationWheel>
{
    public Vector2 normalisedMousePosition;
    public float currentAngle;
    public int hoveredSelection;
    [SerializeField] private int previousHover;
    //private int numOfSelection = 4;
    
    [SerializeField] private GameObject transformWheel;

    [SerializeField] private GameObject lockout;
    [SerializeField] private Image lockoutBar;
    
    public GameObject[] transformationItems; //0[BULLDOZER], 1[FROG], 2[BALL], 3[TERRY]
    public Image[] transformationFills; //0[BULLDOZER], 1[FROG], 2[BALL], 3[TERRY] parallel with transformationItems
    
    private TransformationItem transformation;
    private TransformationItem previousTransformation;
    
    private GameObject smoke;
    private Animator smokeAnimator;

    [Header("Lockout Settings")]
    public float maxLockoutCharge = 100f; //default amount of max charge the player has for transforms
    public Dictionary<Transformation, float> LockoutProgresses = new Dictionary<Transformation, float>(); //the amount of "charge" the player has for each of the transforms
    public float transformCost = 25f; //the amount of "charge" it takes to transform

    [SerializeField] private bool _dbug = false;

    // Toggle for lockout system functionality.
    [SerializeField] private bool lockoutEnabled = true;

    [SerializeField] private AudioData transformationSound;

    // Static key mapping shared across all TransformationWheel instances.
    public static Dictionary<string, Action<TransformationWheel, InputAction.CallbackContext>> staticKeyMapping =
        new Dictionary<string, Action<TransformationWheel, InputAction.CallbackContext>>()
        {
            { "Terry", (wheel, ctx) => wheel.controllerSelect(0) },
            { "Frog", (wheel, ctx) => wheel.controllerSelect(1) },
            { "Bulldozer", (wheel, ctx) => wheel.controllerSelect(2) },
            { "Confirm", (wheel, ctx) => wheel.Transform(ctx) }
        };

    protected override Dictionary<string, Action<TransformationWheel, InputAction.CallbackContext>> KeyMapping => staticKeyMapping;

    // Start is called before the first frame update
    void Start()
    {
        smoke = Player.Instance.transform.Find("Smoke").gameObject;
        smokeAnimator = smoke.GetComponent<Animator>();

        // Enable or disable the lockoutBar based on lockoutEnabled.
        if (!lockoutEnabled)
        {
            lockout.gameObject.SetActive(false);
        }
        else
        {
            lockout.gameObject.SetActive(true);
        }

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
        
        // Radial unit circle based off of screen and mouse position.
        normalisedMousePosition = new Vector2(Input.mousePosition.x - Screen.width/2, 
            Input.mousePosition.y - Screen.height/2);
        
        // Because wheel is rotated add 45 to offset angle.
        currentAngle = Mathf.Atan2(normalisedMousePosition.y, 
            normalisedMousePosition.x) * Mathf.Rad2Deg + 45;
        
        // Bind angle between 0 and 360.
        currentAngle = Mathf.Clamp((currentAngle + 360) % 360, 0, 359);

        Debug.Log("Current Angle: " + currentAngle);
        
        // Create index based off section of wheel over the number of selections.
        //hoveredSelection = (int)currentAngle / (360 / transformationItems.Length);

        if (currentAngle > 80 && currentAngle < 200) {
            hoveredSelection = 2;
        } else if (currentAngle >= 200 && currentAngle < 315) {
            hoveredSelection = 1;
        } else {
            hoveredSelection = 0;
        }

        if (hoveredSelection != previousHover)
        {
            hoverSelect();
        }
    }

    private void InputHandler()
    {
        if (InputManager.Instance && InputManager.Instance.isListening) {
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

        // Lockout functionality: if enabled, prevent transforming when charge is 0 (except for TERRY).
        if (lockoutEnabled)
        {
            if (LockoutProgresses[transformation.GetForm().transformation] <= 0 &&
                form.transformation != Transformation.TERRY)
                return;
        }

        // Play a random transformation sound.
        AudioManager.Instance?.PlayOneShot(transformationSound);

        if (form.transformation != Transformation.BALL) {
            Player.Instance.SetTransformation(form.transformation);
        } else {
            Debug.LogWarning("Ball form is temporarily disabled.");
        }

        transformWheel.SetActive(false);

        // Lockout functionality: subtract lockout progress if not the same transformation.
        if (lockoutEnabled)
        {
            if (previousTransformation != transformation)
                SubtractProgress(form.transformation, transformCost);
        }
        
        Debug.Log("Current: " + transformation.GetForm().transformation + " Previous: " + previousTransformation.GetForm().transformation);
        
        EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = true });
    }

    private void Transform(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Transform();
        }
    }

    private void hoverSelect() {
        // Activate previous hover's animation.
        previousTransformation = transformationItems[previousHover].GetComponent<TransformationItem>();
        previousTransformation.HoverExit();
        previousHover = hoveredSelection;
        
        // Activate current hover's animation.
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
        // If lockout is disabled, do nothing.
        if (!lockoutEnabled) return;

        if (t == Transformation.TERRY)
        {
            lockoutBar.fillAmount = 100;
            return;
        }
        
        Debug.Log("Subtracting from lockout: " + amt + " Current Lockout Charge for " + t + " : " + LockoutProgresses[t]);
        LockoutProgresses[t] -= amt;
        lockoutBar.fillAmount = LockoutProgresses[t] / 100;
        transformationFills[GetIntTransform()].fillAmount = LockoutProgresses[t] / 100;
        if (LockoutProgresses[t] <= 0f) Locked();
    }

    void AddProgress(Transformation t, float amt)
    {
        // If lockout is disabled, do nothing.
        if (!lockoutEnabled) return;

        if (t == Transformation.TERRY)
        {
            lockoutBar.fillAmount = 100;
            return;
        }

        Debug.Log("Adding to lockout: " + amt + " Current Lockout Charge for " + t + " : " + LockoutProgresses[t]);
        LockoutProgresses[t] += amt;
        lockoutBar.fillAmount = LockoutProgresses[t] / 100;
        transformationFills[(int)t].fillAmount = LockoutProgresses[t] / 100;
        if (LockoutProgresses[t] <= maxLockoutCharge) LockoutProgresses[t] = maxLockoutCharge;
    }

    public void ResetProgress()
    {
        // If lockout is disabled, do nothing.
        if (!lockoutEnabled) return;

        foreach (var key in LockoutProgresses.Keys.ToList())
        {
            LockoutProgresses[key] = maxLockoutCharge;
        }
        lockoutBar.fillAmount = 1;
    }

    void SetWheel()
    {
        // Initialize lockout charges if the lockout system is enabled.
        if (lockoutEnabled)
        {
            LockoutProgresses.Add(Transformation.BULLDOZER, maxLockoutCharge);
            LockoutProgresses.Add(Transformation.FROG, maxLockoutCharge);
            //LockoutProgresses.Add(Transformation.BALL, maxLockoutCharge);
            LockoutProgresses.Add(Transformation.TERRY, maxLockoutCharge);

            HandleNulls();
        }
    }

    void Locked()
    {
        // Handle any extra functionalities when locked out.
        Debug.LogWarning("Locked Out!");
    }

    void HandleNulls()
    {
        if (lockout.gameObject == null)
        {
            try
            {
                lockout = GameObject.Find("Lockout Bar Canvas");
            }
            catch
            {
                Debug.LogError("Lockout Bar not set in the inspector");
            }

        }

        if (transformationItems.Length == 0)
        {
            try
            {
                transformationItems[0] = GameObject.Find("Bulldozer Menu Form");
                transformationItems[1] = GameObject.Find("Frog Menu Form");
                transformationItems[2] = GameObject.Find("Boulder Menu Form");
                transformationItems[3] = GameObject.Find("Terry Menu Form");
            }
            catch
            {
                Debug.LogError("Transformation Items not set in the inspector");
            }
        }

        if (transformationFills.Length == 0)
        {
            try
            {
                transformationFills[0] = GameObject.Find("Fill Charge Bulldozer").GetComponent<Image>();
                transformationFills[1] = GameObject.Find("Fill Charge Frog").GetComponent<Image>();
                transformationFills[2] = GameObject.Find("Fill Charge Boulder").GetComponent<Image>();
                transformationFills[3] = GameObject.Find("Fill Charge Terry").GetComponent<Image>();
            }
            catch
            {
                Debug.LogError("Transformation Fills not set in the inspector");
            }
        }
    }
}
