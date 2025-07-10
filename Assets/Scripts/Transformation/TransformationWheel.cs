using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;
using UnityEditor.Rendering.Universal;
using UnityEngine.SceneManagement;

public class TransformationWheel : KeyActionReceiver<TransformationWheel>
{
    public Vector2 normalisedMousePosition;
    public float currentAngle;
    public int hoveredSelection;
    [SerializeField] private int previousHover;

    public bool isLockedOut => IsLockedOut();
    //private int numOfSelection = 4;
    
    [SerializeField] private GameObject transformWheel;

    [SerializeField] private GameObject lockout;
    [SerializeField] private Image lockoutBar;
    
    public GameObject[] transformationItems; //0[BULLDOZER], 1[FROG], 2[BALL], 3[TERRY]
    public Image[] transformationFills; //0[BULLDOZER], 1[FROG], 2[BALL], 3[TERRY] parallel with transformationItems
    
    private TransformationItem transformation;
    private TransformationItem previousTransformation;

    private Form currForm;
    private Form prevForm;
    
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
    [SerializeField] private GameObject softlockNotification;

    // Static key mapping shared across all TransformationWheel instances.
    public static Dictionary<string, Action<TransformationWheel, InputAction.CallbackContext>> staticKeyMapping
        = new Dictionary<string, Action<TransformationWheel, InputAction.CallbackContext>>()
    {
        { "Bulldozer", (w, ctx) => w.HandleControllerSelection(ctx, 0) },
        { "Frog",      (w, ctx) => w.HandleControllerSelection(ctx, 1) },
        { "Terry",     (w, ctx) => w.HandleControllerSelection(ctx, 2) },

        { "Confirm",   (w, ctx) => w.OnConfirm(ctx) },

        { "Cancel",    (w, ctx) => w.OnCancel(ctx) },

        { "NavigateWheel", (w, ctx) => w.OnNavigate(ctx) },
    };


    protected override Dictionary<string, Action<TransformationWheel, InputAction.CallbackContext>> KeyMapping
    {
        get { return staticKeyMapping; }
    }

    // Start is called before the first frame update
    void Start()
    {
        RechargeStation.OnRechargeStation += AddProgressToAllForms;
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

    private void OnDestroy() {
        RechargeStation.OnRechargeStation -= AddProgressToAllForms;
    }

    public static event Action<Transformation> TransformedObjective;
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

        Form form = transformation.GetForm();

        // Lockout functionality: if enabled, prevent transforming when charge is 0 (except for TERRY).
        if (lockoutEnabled)             
        {
            if (LockoutProgresses[transformation.GetForm().transformation] <= 0 &&
                form.transformation != Transformation.TERRY)
                return;
        }

        // Play a random transformation sound.
        AudioManager.Instance?.PlayOneShot(transformationSound);

        //TRANSFORMATION LOGIC
        if (form.transformation != Transformation.BALL) {
            Player.Instance.SetTransformation(form.transformation);
            
            //listened to by 'TransformationSwapInteractObjective.cs'
            TransformedObjective?.Invoke(transformation.GetForm().transformation);
        } else {
            Debug.LogWarning("Ball form is temporarily disabled.");
        }
    
        transformWheel.SetActive(false);

        // Lockout functionality: subtract lockout progress if not the same transformation.
        if (lockoutEnabled)
        {
            if (Player.Instance.prevTransformation != Player.Instance.GetTransformation())
                SubtractProgress(form.transformation, transformCost);
        }
        
        Debug.Log("Current: " + form.transformation + " Previous: " + previousTransformation.GetForm().transformation);
        
        EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = true });
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

    private void controllerSelect(int selection)
    {
        // 1) remember the old hover, update to the new one
        previousHover    = hoveredSelection;
        hoveredSelection = selection;

        // 2) now do the enter/exit animations
        hoverSelect();
    }

    // Centralized D‑pad handler
    private void HandleControllerSelection(InputAction.CallbackContext ctx, int selection)
    {
        if (!ctx.performed) return;

        // 2) Move the hover
        controllerSelect(selection);
    }

    // Confirm → do the transform (and inside Transform() you already deactivate the wheel)
    private void OnConfirm(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Transform();
    }

    // Cancel → forcibly close the wheel & re‑enable movement
    private void OnCancel(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (!transformWheel.activeSelf) return;

        transformWheel.SetActive(false);
        EventDispatcher.Raise<TogglePlayerMovement>(
            new TogglePlayerMovement() { isEnabled = true }
        );
    }

    // Left‑stick navigation: behaves almost exactly like D‑pad
    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        Vector2 v = ctx.ReadValue<Vector2>();
        if (v.x < -0.5f) HandleControllerSelection(ctx, 1);    // left
        else if (v.y >  0.5f) HandleControllerSelection(ctx, 2); // up
        else if (v.x >  0.5f) HandleControllerSelection(ctx, 0); // right
    }

    int GetIntTransform(Transformation t)
    {
        // 0[BULLDOZER], 1[FROG], 2[BALL], 3[TERRY]
        if (t == Transformation.BULLDOZER) return 0;
        else if (t == Transformation.FROG) return 1;
        //else if (current == Transformation.BALL) return 2;
        else if (t == Transformation.TERRY) return 2;
        else return 0;
    }

    public void SubtractProgress(Transformation t, float amt)
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
        transformationFills[GetIntTransform(t)].fillAmount = LockoutProgresses[t] / 100;
        if (LockoutProgresses[t] <= 0f) LockedOut();
        
        if (isLockedOut) LockedOut();
    }

    public bool breakSoftLock;
    public void SoftLockProtocol()
    {
        if (!breakSoftLock) return;
        Debug.LogWarning("Player got softlocked, restarting scene");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        ResetProgress();
        softlockNotification.SetActive(false);
        Player.Instance.SetTransformation(Transformation.TERRY);
        //add obj tracker reset
    }

    public void AddProgress(Transformation t, float amt)
    {
        // If lockout is disabled, do nothing.
        if (!lockoutEnabled) return;
        if (!LockoutProgresses.ContainsKey(t)) return;

        if (t == Transformation.TERRY)
        {
            lockoutBar.fillAmount = 100;
            return;
        }

        Debug.Log("Adding to lockout: " + amt + " Current Lockout Charge for " + t + " : " + LockoutProgresses[t]);
        LockoutProgresses[t] += amt;
        lockoutBar.fillAmount = LockoutProgresses[t] / 100;
        transformationFills[GetIntTransform(t)].fillAmount = LockoutProgresses[t] / 100;
        if (LockoutProgresses[t] >= maxLockoutCharge) LockoutProgresses[t] = maxLockoutCharge;
        
        if(!isLockedOut) LockedOut(false);
    }

    private void DisplayLockedOutNotification(bool set = true)
    {
        softlockNotification.SetActive(set);
    }

    private bool IsLockedOut()
    {
        bool isLockedOut = false;
        foreach (var x in LockoutProgresses)
        {
            if (x.Key == Transformation.TERRY) continue;
            if (x.Value == 0) continue;
            else
            {
                isLockedOut = false;
                break;
            }
        }
        return isLockedOut;
    }
    

    public void AddProgressToAllForms(float customCharge = 100f) {
        AddProgress(Transformation.BULLDOZER, customCharge);
        AddProgress(Transformation.FROG, customCharge);
    }

    public void ResetProgress()
    {
        // If lockout is disabled, do nothing.
        if (!lockoutEnabled) return;
        Debug.LogWarning("Resetting Transform Wheel Progress...");

        LockoutProgresses.Clear();
        SetWheel();
        lockoutBar.fillAmount = 1;
    }

    void SetWheel()
    {
        // Initialize lockout charges if the lockout system is enabled.
        if (!lockoutEnabled) return;

        LockoutProgresses.TryAdd(Transformation.BULLDOZER, maxLockoutCharge);
        LockoutProgresses.TryAdd(Transformation.FROG, maxLockoutCharge);
        LockoutProgresses.TryAdd(Transformation.TERRY, maxLockoutCharge);

        HandleNulls();
        SetWheelUI();
    }

    void SetWheelUI()
    {
        foreach(var x in transformationFills)
        {
           //Debug.Log(x.name);
            x.fillAmount = 1;
        }
    }

    void LockedOut(bool set = true)
    {
        DisplayLockedOutNotification(set);
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
                //transformationItems[2] = GameObject.Find("Boulder Menu Form");
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
                //transformationFills[2] = GameObject.Find("Fill Charge Boulder").GetComponent<Image>();
                transformationFills[3] = GameObject.Find("Fill Charge Terry").GetComponent<Image>();
            }
            catch
            {
                Debug.LogError("Transformation Fills not set in the inspector");
            }
        }
    }
}
