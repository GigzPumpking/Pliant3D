using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.InputSystem;

public class TransformationWheel : MonoBehaviour
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

    public PlayerControls playerControls;

    private InputAction _transform;

    void Awake()
    {
        playerControls = new PlayerControls();
    }

    void OnEnable()
    {
        _transform = playerControls.Player.Transform;
        _transform.performed += ctx => DisableWheel();
        _transform.performed += ctx => Transform();
        _transform.Enable();
    }

    void OnDisable()
    {
        _transform.performed -= ctx => DisableWheel();
        _transform.performed -= ctx => Transform();
        _transform.Disable();
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
        
        // if hovered isn't the same as previous hovered then selection is made
        if (hoveredSelection != previousHover)
        {
            //activate previous hover's animation
            previousTransformation = transformationItems[previousHover].GetComponent<TransformationItem>();
            previousTransformation.HoverExit();
            previousHover = hoveredSelection;
            
            //activate current hover's animaton.
            transformation = transformationItems[hoveredSelection].GetComponent<TransformationItem>();
            transformation.HoverEnter();
        }

        /*

        if (Input.GetMouseButtonDown(0))
        {
            var form = transformation.GetForm();
            
            Player.Instance.SetTransformation(form.transformation);
            transformWheel.SetActive(false);
            
            EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = true });
        }

        */

        
        // Debug.Log(hoveredSelection);
    }

    private void DisableWheel()
    {
        transformWheel.SetActive(false);

        EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = true });
    }

    private void Transform()
    {
        var form = transformation.GetForm();
        
        Player.Instance.SetTransformation(form.transformation);
        transformWheel.SetActive(false);
        
        EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = true });
    }
    
}
