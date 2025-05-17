using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum Directions {
    UP,
    DOWN,
    LEFT,
    RIGHT
}

public class Player : KeyActionReceiver<Player>
{
    // Instance 
    private static Player instance;
    public static Player Instance { get { return instance; } }

    // Collision Variables
    private Rigidbody rbody;

    // Animation Variables
    private Animator animator;
    private Animator smokeAnimator;
    private Transform sparkles;
    private Animator sparklesAnimator;

    bool isMoving = false;

    public Directions FacingDirection => facingDirection;

    // backing field for the above
    private Directions facingDirection = Directions.DOWN;  

    // track last pure horizontal or vertical intent
    private enum Axis { None, Horizontal, Vertical }
    private Axis lastDominantAxis = Axis.Horizontal;

    // to detect edges
    private bool prevH = false;
    private bool prevV = false;

    // track last non‑zero horizontal so we know which way “left” refers to
    private Directions lastHorizontalInput = Directions.RIGHT;

    [SerializeField] bool isGrounded = true;

    // Jumping and Movement Variables
    [SerializeField] float movementSpeed = 5f;
    private Vector2 movementInput;
    float timeElapsed = 0f;
    public bool canMove = true;

    // Transformation Variables
    public Transformation transformation = Transformation.TERRY;
    private Transform transformationWheel;
    private TransformationWheel transformationWheelScript;
    private Dictionary<Transformation, (Transform group, SpriteRenderer sprite, Animator animator, FormScript script)> transformationMapping;
    private Transform smoke;
    private Transform shadow;
    private Transform terryGroup;
    private Transform frogGroup;
    private Transform bulldozerGroup;
    private Transform ballGroup;
    private Transform selectedGroup;
    private SpriteRenderer selectedGroupSprite;
    private FormScript selectedGroupScript;

    public float transformationDuration = 10f;

    // Other Variables
    [SerializeField] private float outOfBoundsY = -10f;

    [SerializeField] private float minMoveThreshold = 0.05f;

    [SerializeField] private Vector3[] areaPositions;

    [SerializeField] private bool _dbug = false;

    // Static key mapping shared across all Player instances.
    public static Dictionary<string, Action<Player, InputAction.CallbackContext>> staticKeyMapping =
        new Dictionary<string, Action<Player, InputAction.CallbackContext>>()
        {
            { "Move", (instance, ctx) => instance.setMovementInput(ctx) },
            { "Transform", (instance, ctx) => instance.TransformationHandler(ctx) },
            { "TransformKeyboard", (instance, ctx) => instance.TransformationKeyboardHandler(ctx) },
            { "Interact", (instance, ctx) => instance.InteractHandler(ctx) },
            { "Ability1", (instance, ctx) => instance.Ability1Handler(ctx) },
            { "Ability2", (instance, ctx) => instance.Ability2Handler(ctx) },
            { "Ability3", (instance, ctx) => instance.Ability3Handler(ctx) }
        };

    protected override Dictionary<string, Action<Player, InputAction.CallbackContext>> KeyMapping => staticKeyMapping;

    protected override void OnEnable()
    {
        base.OnEnable();
        EventDispatcher.AddListener<StressDebuff>(StressDebuffHandler);
        EventDispatcher.AddListener<TogglePlayerMovement>(e => canMoveToggle(e.isEnabled));
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventDispatcher.RemoveListener<StressDebuff>(StressDebuffHandler);
        EventDispatcher.RemoveListener<TogglePlayerMovement>(e => canMoveToggle(e.isEnabled));
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            // move correct instance to this instance's location before destroying
            instance.transform.position = this.transform.position;
            instance.setAreas(areaPositions);
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        rbody = GetComponent<Rigidbody>();

        InitializeTransformations();

        SetTransformation(Transformation.TERRY);

        smoke = transform.Find("Smoke");
        smokeAnimator = smoke.GetComponent<Animator>();
        sparkles = transform.Find("Sparkles");
        sparklesAnimator = sparkles.GetComponent<Animator>();
        sparkles.gameObject.SetActive(false);
        smoke.gameObject.SetActive(false);
        transformationWheel = transform.Find("Transformation Wheel");
        transformationWheelScript = transformationWheel.GetComponentInChildren<TransformationWheel>();

        if (animator != null) {
            animator.SetBool("isWalking", false);

            // Default to down
            animator.SetFloat("MoveX", -1);
            animator.SetFloat("MoveY", -1);
        }

        EventDispatcher.Raise<DebugMessage>(new DebugMessage() { message = "Player Initialized" });
    }

    public void stopMovement() {
        rbody.velocity = Vector3.zero;
        movementInput = Vector2.zero;

        animator?.SetBool("isWalking", false);
    }

    public GameObject GetSmoke() {
        return smoke.gameObject;
    }

    void InteractHandler(InputAction.CallbackContext context) {
        if (context.performed && transformation == Transformation.TERRY) {
            //Debug.LogError("Raising the regular interact event");
            EventDispatcher.Raise<Interact>(new Interact());

            //WITHIN THE OBJECTIVE INTERACTABLE SPACE
            if (inObjectiveInteractable)
            {
                //Debug.LogError("Raising the objective interact event " + lastObjectiveInteractable.name);
                ObjectiveInteractEvent interact = new ObjectiveInteractEvent();
                interact.currentTransformation = transformation;
                interact.interactedTo = lastObjectiveInteractable;
                EventDispatcher.Raise<ObjectiveInteractEvent>(interact);
            }
        }
    }

    bool inObjectiveInteractable;
    GameObject lastObjectiveInteractable;
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Objective Interactable")
        {
            //Debug.LogError("Walked into an objective zone: " + other.gameObject.name);
            inObjectiveInteractable = true;
            lastObjectiveInteractable = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Objective Interactable")
        {
            inObjectiveInteractable = false;
        }
    }

    void Update() {
        // Animations + Input

        if (canMove) {
            InputHandler();
            MoveHandler();
        } else {
            isMoving = false;
        }

        if (transform.position.y < outOfBoundsY) {
            resetPosition();
        }
    }

    public void resetPosition() {
        transform.position = areaPositions[0];
    }

    void setMovementInput(InputAction.CallbackContext context) {
        EventDispatcher.Raise<DebugMessage>(new DebugMessage() { message = $"Setting movement input: {context.ReadValue<Vector2>()}" });

        // if in the air while being a bulldozer, don't move
        if (!isGrounded && transformation == Transformation.BULLDOZER) {
            movementInput = Vector2.zero;
            return;
        }

        // Use InputManager or another source to get the current movement vector
        Vector2 moveValue = InputManager.Instance.isListening
            ? context.ReadValue<Vector2>()
            : Vector2.zero;

        if (moveValue.x < minMoveThreshold && moveValue.x > -minMoveThreshold) {
            moveValue.x = 0;
        } 

        if (moveValue.y < minMoveThreshold && moveValue.y > -minMoveThreshold) {
            moveValue.y = 0;
        }

        movementInput = moveValue;
    }


    void MoveHandler() {
        float vx = movementInput.x;
        float vy = movementInput.y;
        float thr = minMoveThreshold;

        bool h = Mathf.Abs(vx) >= thr;
        bool v = Mathf.Abs(vy) >= thr;

        // ——— Edge detection: did H or V just go from “off”→“on”? ———
        if (h && !prevH) lastDominantAxis = Axis.Horizontal;
        if (v && !prevV) lastDominantAxis = Axis.Vertical;

        prevH = h;
        prevV = v;

        // ——— Now decide facing based on the most‐recently pressed axis ———
        if (h && lastDominantAxis == Axis.Horizontal)
        {
            // pure left/right
            facingDirection     = vx > 0 ? Directions.RIGHT : Directions.LEFT;
            lastHorizontalInput = facingDirection;
        }
        else if (v && lastDominantAxis == Axis.Vertical)
        {
            // vertical → map into your isometric cross
            if (vy > 0)
                // “up” arm: top‑left or top‑right
                facingDirection = (lastHorizontalInput == Directions.LEFT)
                                 ? Directions.UP
                                 : Directions.RIGHT;
            else
                // “down” arm: bottom‑left or bottom‑right
                facingDirection = (lastHorizontalInput == Directions.LEFT)
                                 ? Directions.LEFT
                                 : Directions.DOWN;
        }

        // flip sprite on horizontal input
        if (h)
            selectedGroup.GetComponentInChildren<SpriteRenderer>().flipX = (vx > 0);

        // feed animator
        if (animator != null)
        {
            bool isMoving = vx != 0 || vy != 0;
            if (isMoving) {
                animator.SetFloat("MoveX", vx);
                animator.SetFloat("MoveY", 3*vy);
            }
            animator.SetBool("isWalking", vx != 0 || vy != 0);
        }

        // actual movement
        Vector3 camF = Camera.main.transform.forward; camF.y = 0; camF.Normalize();
        Vector3 camR = Camera.main.transform.right;   camR.y = 0; camR.Normalize();
        Vector3 dir  = (camF * vy + camR * vx).normalized;
        rbody.velocity = new Vector3(
            dir.x * movementSpeed,
            rbody.velocity.y,
            dir.z * movementSpeed
        );
    }

    public void SetVelocity(Vector3 velocity) {
        rbody.velocity = velocity;
    }

    public void SetSpeed(float speed) {
        movementSpeed = speed;
    }

    void Ability1Handler(InputAction.CallbackContext context) {
        if (UIManager.Instance && UIManager.Instance.isPaused) return;
        selectedGroup.GetComponent<FormScript>().Ability1(context);
    }

    void Ability2Handler(InputAction.CallbackContext context) {
        if (UIManager.Instance && UIManager.Instance.isPaused) return;
        selectedGroup.GetComponent<FormScript>().Ability2(context);
    }

    void Ability3Handler(InputAction.CallbackContext context) {
        if (UIManager.Instance && UIManager.Instance.isPaused) return;
        selectedGroup.GetComponent<FormScript>().Ability3(context);
    }

    void InputHandler() {
        // For each of the areas, check Alpha1, Alpha2, ... until area's length
        for (int i = 1; i <= areaPositions.Length; i++) {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i)) {
                moveToArea(i - 1);
            }
        }

        // If Input System is disabled, use Input.GetKey
        if (InputManager.Instance && InputManager.Instance.isListening) {
            return;
        }

        // if E is pressed, interact with object
        if (Input.GetKeyDown(KeyCode.E)) {
            EventDispatcher.Raise<Interact>(new Interact());
        }
    }

    public void TransformationHandler() {
        if (!isGrounded || (UIManager.Instance && (UIManager.Instance.isPaused || UIManager.Instance.isDialogueActive)) && transformationWheel.gameObject.activeSelf) return;

        transformationWheel.gameObject.SetActive(true);
        canMoveToggle(false);
    }

    public void TransformationHandler(InputAction.CallbackContext context) {
        if (context.performed) {
            TransformationHandler();
        }
    }

    public void TransformationKeyboardHandler(InputAction.CallbackContext context) {
        if (context.performed) {
            if (!isGrounded || (UIManager.Instance && (UIManager.Instance.isPaused || UIManager.Instance.isDialogueActive))) return;

            transformationWheel.gameObject.SetActive(!transformationWheel.gameObject.activeSelf);
            canMoveToggle(!transformationWheel.gameObject.activeSelf);
        }
    }

    public bool TransformationChecker() {
        return (transformationWheel.gameObject.activeSelf);
    }

    private void InitializeTransformations()
    {
        terryGroup = transform.Find("Terry");
        frogGroup = transform.Find("Frog");
        bulldozerGroup = transform.Find("Bulldozer");
        ballGroup = transform.Find("Ball");

        transformationMapping = new Dictionary<Transformation, (Transform, SpriteRenderer, Animator, FormScript)>()
        {
            { Transformation.TERRY, (terryGroup, terryGroup.GetComponentInChildren<SpriteRenderer>(), terryGroup.GetComponentInChildren<Animator>(), terryGroup.GetComponentInChildren<FormScript>()) },
            { Transformation.FROG, (frogGroup, frogGroup.GetComponentInChildren<SpriteRenderer>(), frogGroup.GetComponentInChildren<Animator>(), frogGroup.GetComponentInChildren<FormScript>()) },
            { Transformation.BULLDOZER, (bulldozerGroup, bulldozerGroup.GetComponentInChildren<SpriteRenderer>(), bulldozerGroup.GetComponentInChildren<Animator>(), bulldozerGroup.GetComponentInChildren<FormScript>()) },
            { Transformation.BALL, (ballGroup, ballGroup.GetComponentInChildren<SpriteRenderer>(), ballGroup.GetComponentInChildren<Animator>(), ballGroup.GetComponentInChildren<FormScript>()) },
        };
    }

    public void SetTransformation(Transformation newTransformation)
    {
        if (transformation != newTransformation)
        {
            Smoke(); // Play transformation smoke effect
        }

        transformation = newTransformation;

        // Deactivate all groups
        foreach (var entry in transformationMapping.Values)
        {
            entry.group.gameObject.SetActive(false);
        }

        // Activate the selected transformation group
        if (transformationMapping.TryGetValue(transformation, out var selected))
        {
            selected.group.gameObject.SetActive(true);
            selectedGroup = selected.group;
            selectedGroupSprite = selected.sprite;
            animator = selected.animator;
            selectedGroupScript = selected.script;
            movementSpeed = selectedGroupScript.GetSpeed();
        }
        else
        {
            Debug.LogWarning($"Transformation {transformation} not found in mapping! Defaulting to TERRY.");
            SetTransformation(Transformation.TERRY);
        }
    }

    public Transformation GetTransformation() {
        return transformation;
    }

    public void StressDebuffHandler(StressDebuff e) {
        SetTransformation(Transformation.TERRY);
    }

    public void Smoke() {
        if (!smoke) return;
        smoke.gameObject.SetActive(true);
        smokeAnimator.Play("Smoke");
    }

    public void HealAnim() {
        sparkles.gameObject.SetActive(true);
        sparklesAnimator.Play("Heal Anim");
    }

    public void Heal() {
        EventDispatcher.Raise<Heal>(new Heal());
        HealAnim();
    }

    public void moveToArea(int area) {
        transform.position = areaPositions[area];
    }

    private void setAreas(Vector3[] positions) {
        areaPositions = positions;
    }

    public void canMoveToggle(bool toggle) {
        canMove = toggle;
        if (!canMove) {
            stopMovement();
        }
    }
}
