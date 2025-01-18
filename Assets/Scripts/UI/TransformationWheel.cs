using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.InputSystem;

public class TransformationWheel : MonoBehaviour, IKeyActionReceiver
{
    public Vector2 normalisedMousePosition;
    public float currentAngle;
    public int hoveredSelection;
    [SerializeField] private int previousHover;
    //private int numOfSelection = 4;
    
    [SerializeField] private GameObject transformWheel;
    
    public GameObject[]  transformationItems;
    
    private TransformationItem transformation;
    private TransformationItem previousTransformation;
    
    private GameObject smoke;
    private Animator smokeAnimator;

    // Dictionary for mapping actions to functions
    private Dictionary<string, System.Action<InputAction.CallbackContext>> actionMap;

    [SerializeField] private bool _dbug = false;

    private void RegisterInputActions()
    {
        foreach (var action in actionMap.Keys)
        {
            InputManager.Instance.AddKeyBind(this, action, "Gameplay");
        }
    }

    private void OnDestroy()
    {
        foreach (var action in actionMap.Keys)
        {
            InputManager.Instance.RemoveKeyBind(this, action);
        }
    }

    private void InitializeActionMap()
    {
        actionMap = new Dictionary<string, Action<InputAction.CallbackContext>>()
        {
            { "Ball", ctx => controllerSelect(0) },
            { "Frog", ctx => controllerSelect(1) },
            { "Bulldozer", ctx => controllerSelect(2) },
            { "Terry", ctx => controllerSelect(3) },
            { "Confirm", ctx => { if (ctx.performed) Transform(); } }
        };
    }

    void Awake()
    {
        InitializeActionMap();
        RegisterInputActions();
    }

    // Start is called before the first frame update
    void Start()
    {
        smoke = Player.Instance.transform.Find("Smoke").gameObject;
        smokeAnimator = smoke.GetComponent<Animator>();
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
        
        Player.Instance.SetTransformation(form.transformation);
        transformWheel.SetActive(false);
        
        EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = true });
    }

    public void OnKeyAction(string action, InputAction.CallbackContext context)
    {
        if (actionMap.TryGetValue(action, out var actionHandler))
        {
            actionHandler(context);
        }
        else
        {
            Debug.LogWarning($"Unhandled action: {action}");
        }
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
    
}
