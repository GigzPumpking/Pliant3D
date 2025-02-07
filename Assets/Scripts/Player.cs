using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, IKeyActionReceiver
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

    enum Directions {
        UP,
        DOWN,
        LEFT,
        RIGHT
    }

    private Directions lastVerticalInput = Directions.DOWN;
    private Directions lastHorizontalInput = Directions.RIGHT;

    bool isMoving = false;
    private Directions lastInput = Directions.DOWN;

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

    [SerializeField] private Vector3[] areaPositions;

    public bool directionalMovement = false;

    // Dictionary for mapping actions to functions
    private Dictionary<string, System.Action<InputAction.CallbackContext>> actionMap;

    [SerializeField] private bool _dbug = false;

    void OnEnable()
    {
        EventDispatcher.AddListener<StressDebuff>(StressDebuffHandler);
        EventDispatcher.AddListener<TogglePlayerMovement>(e => canMoveToggle(e.isEnabled));
    }

    void OnDisable()
    {
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

        InitializeActionMap();
        transformationWheelScript.InitializeActionMap();

        EventDispatcher.Raise<DebugMessage>(new DebugMessage() { message = "Player Initialized" });
    }

    private void InitializeActionMap()
    {
        EventDispatcher.Raise<DebugMessage>(new DebugMessage() { message = "Initializing Player Input" });

        actionMap = new Dictionary<string, System.Action<InputAction.CallbackContext>>()
        {
            { "Move", ctx => { setMovementInput(ctx); } },
            { "Transform", ctx => { if (ctx.performed) TransformationHandler(); } },
            { "Interact", ctx => { if (ctx.performed) EventDispatcher.Raise<Interact>(new Interact()); } },
            { "Ability1", ctx => { Ability1Handler(ctx); } },
            { "Ability2", ctx => { Ability2Handler(ctx); } }
        };

        foreach (var action in actionMap.Keys)
        {
            EventDispatcher.Raise<DebugMessage>(new DebugMessage() { message = $"Adding keybind for {action} to action map" });
            InputManager.Instance.AddKeyBind(this, action, "Gameplay");
        }
    }

    public GameObject GetSmoke() {
        return smoke.gameObject;
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
            transform.position = areaPositions[0];
        }
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

    void setMovementInput(InputAction.CallbackContext context) {
        EventDispatcher.Raise<DebugMessage>(new DebugMessage() { message = $"Setting movement input: {context.ReadValue<Vector2>()}" });

        // Use InputManager or another source to get the current movement vector
        Vector2 moveValue = InputManager.Instance.IsInputEnabled
            ? context.ReadValue<Vector2>()
            : Vector2.zero;

        if (moveValue.x > 0.05f) {
            moveValue.x = 1;
        } else if (moveValue.x < -0.05f) {
            moveValue.x = -1;
        } else {
            moveValue.x = 0;
        }

        if (moveValue.y > 0.05f) {
            moveValue.y = 1;
        } else if (moveValue.y < -0.05f) {
            moveValue.y = -1;
        } else {
            moveValue.y = 0;
        } 

        movementInput = moveValue;
    }


    void MoveHandler() {
        float verticalInput = movementInput.y;
        float horizontalInput = movementInput.x;

        if (!InputManager.Instance.IsInputEnabled) {
            verticalInput = Input.GetAxis("Vertical");
            horizontalInput = Input.GetAxis("Horizontal");
            Debug.Log("Input System Disabled");
        }

        Vector3 cameraForwards = Camera.main.transform.forward;

        Vector3 cameraRight = Camera.main.transform.right;

        cameraForwards.y = 0f;
        cameraRight.y = 0f;
        cameraForwards = cameraForwards.normalized;
        cameraRight = cameraRight.normalized;

        Vector3 desiredMoveDirection = Vector3.zero;

        if (directionalMovement) {
            if (verticalInput != 0) {
                if (verticalInput > 0) {
                    verticalInput = 1;
                    selectedGroupSprite.flipX = false;
                } else {
                    verticalInput = -1;
                    selectedGroupSprite.flipX = true;
                }

                horizontalInput = 0;
            }
            if (horizontalInput != 0) {
                if (horizontalInput > 0) {
                    horizontalInput = 1;
                    selectedGroupSprite.flipX = true;
                } else {
                    horizontalInput = -1;
                    selectedGroupSprite.flipX = false;
                }
                verticalInput = 0;
            }

            desiredMoveDirection = Vector3.forward * verticalInput + Vector3.right * horizontalInput;
        } else {
            desiredMoveDirection = cameraForwards * verticalInput + cameraRight * horizontalInput;
        }

        if (animator != null) {
            if (horizontalInput != 0 || verticalInput != 0){
                animator.SetFloat("MoveX", horizontalInput);
                animator.SetFloat("MoveY", verticalInput);
            }
        }

        desiredMoveDirection = desiredMoveDirection.normalized;

        isMoving = desiredMoveDirection != Vector3.zero ? true : false;

        if (animator != null) {
            animator.SetBool("isWalking", isMoving);
        }

        if (movementInput.x < 0) {
            if (!directionalMovement) {
                selectedGroup.GetComponentInChildren<SpriteRenderer>().flipX = false;
            }
            lastHorizontalInput = Directions.LEFT;
            lastInput = Directions.LEFT;
        } else if (movementInput.x > 0) {
            if (!directionalMovement) {
                selectedGroup.GetComponentInChildren<SpriteRenderer>().flipX = true;
            }
            lastHorizontalInput = Directions.RIGHT;
            lastInput = Directions.RIGHT;
        }

        if (movementInput.y < 0) {
            lastVerticalInput = Directions.UP;
            lastInput = Directions.UP;
        } else if (movementInput.y > 0) {
            lastVerticalInput = Directions.DOWN;
            lastInput = Directions.DOWN;
        }

        rbody.velocity = new Vector3(desiredMoveDirection.x * movementSpeed, rbody.velocity.y, desiredMoveDirection.z * movementSpeed);
    }

    public void SetVelocity(Vector3 velocity) {
        rbody.velocity = velocity;
    }

    public void SetSpeed(float speed) {
        movementSpeed = speed;
    }

    void Ability1Handler(InputAction.CallbackContext context) {
        selectedGroup.GetComponent<FormScript>().Ability1(context);
    }

    void Ability2Handler(InputAction.CallbackContext context) {
        selectedGroup.GetComponent<FormScript>().Ability2(context);
    }

    void InputHandler() {
        // For each of the areas, check Alpha1, Alpha2, ... until area's length
        for (int i = 1; i <= areaPositions.Length; i++) {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i)) {
                moveToArea(i - 1);
            }
        }

        // If Input System is disabled, use Input.GetKey
        if (InputManager.Instance.IsInputEnabled) {
            return;
        }

        // if E is pressed, interact with object
        if (Input.GetKeyDown(KeyCode.E)) {
            EventDispatcher.Raise<Interact>(new Interact());
        }
    }

    public void TransformationHandler() {
        transformationWheel.gameObject.SetActive(!transformationWheel.gameObject.activeSelf);
        canMove = !transformationWheel.gameObject.activeSelf;
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

    public void ToggleMovement() {
        directionalMovement = !directionalMovement;
    }

    public void canMoveToggle(bool toggle) {
        canMove = toggle;
        animator.SetBool("isWalking", false);
    }
}
