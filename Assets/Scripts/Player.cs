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

    private bool pushState = false;
    public GameObject obstacleToBreak;
    public GameObject obstacleToPull;
    public Transform objectToHookTo;
    public float hookForce = 0.5f;
    public float hookDuration = 3f;
    public bool usePhysicsHooking = false;
    public Color obstacleToPullColor;
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

        if (animator != null) {
            animator.SetBool("isWalking", false);

            // Default to down
            animator.SetFloat("MoveX", -1);
            animator.SetFloat("MoveY", -1);
        }

        InitializeActionMap();
        RegisterInputActions();
    }

    private void RegisterInputActions()
    {
        foreach (var action in actionMap.Keys)
        {
            InputManager.Instance.AddKeyBind(this, action, "Gameplay");
        }
    }

    private void InitializeActionMap()
    {
        actionMap = new Dictionary<string, System.Action<InputAction.CallbackContext>>()
        {
            { "Move", ctx => { setMovementInput(ctx); } },
            { "Transform", ctx => { if (ctx.performed) TransformationHandler(); } },
            { "Interact", ctx => { if (ctx.performed) EventDispatcher.Raise<Interact>(new Interact()); } },
            { "Ability1", ctx => { Ability1Handler(ctx); } },
            { "Ability2", ctx => { Ability2Handler(ctx); } }
        };
    }

    private void OnDestroy()
    {
        foreach (var action in actionMap.Keys)
        {
            InputManager.Instance.RemoveKeyBind(this, action);
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

    // Update is called once per frame
    void FixedUpdate()
    {
        // Physics + Rigidbodies/Colliders + Applying Input
        PullChecker();
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

    }

    private void setPushState(bool state) {
        if (pushState != state) {
            pushState = state;

            if (state == true) {
                rbody.mass = 1000;
                EventDispatcher.Raise<ShiftAbility>(new ShiftAbility() {
                    isEnabled = true,
                    transformation = Transformation.BULLDOZER
                });
            } else {
                rbody.mass = 1;
                EventDispatcher.Raise<ShiftAbility>(new ShiftAbility() {
                    isEnabled = false
                });
            }
        }
    }

    void PullChecker() {
        // Pullable Layer
        int layerMask = 1 << 6;

        RaycastHit hit;

        Vector3 position = transform.position;
        position.y += 0.5f;

        Vector3 facingDirection = transform.TransformDirection(Vector3.forward);
        switch (lastInput) {
            case Directions.UP:
                facingDirection = transform.TransformDirection(Vector3.forward);
                break;
            case Directions.DOWN:
                facingDirection = transform.TransformDirection(Vector3.back);
                break;
            case Directions.LEFT:
                facingDirection = transform.TransformDirection(Vector3.left);
                break;
            case Directions.RIGHT:
                facingDirection = transform.TransformDirection(Vector3.right);
                break;
        }

        if (Physics.Raycast(position, facingDirection, out hit, 10f, layerMask)) 
        {
            Debug.DrawRay(position, facingDirection * hit.distance, Color.red);
            if (hit.collider.gameObject != obstacleToPull && transformation == Transformation.FROG) {
                SetPullingTarget(hit.collider.gameObject);
            }
        } else {
            Debug.DrawRay(position, facingDirection * hit.distance, Color.red);
            SetPullingTarget(null);
        }
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

        if (Input.GetKeyDown(KeyCode.G) && transformation == Transformation.FROG) {
            if (objectToHookTo != null) {
                GrapplingHook(objectToHookTo);
                EventDispatcher.Raise<StressAbility>(new StressAbility());
            }
        }

        // if E is pressed, interact with object
        if (Input.GetKeyDown(KeyCode.E)) {
            EventDispatcher.Raise<Interact>(new Interact());
        }

        // if F is pressed while in Bulldozer form, break the obstacle
        if (Input.GetKeyDown(KeyCode.F) && transformation == Transformation.BULLDOZER && obstacleToBreak != null) {
            obstacleToBreak.SetActive(false);
            obstacleToBreak = null;
            EventDispatcher.Raise<StressAbility>(new StressAbility());
        }

        // if F is pressed while in Frog form, pull the obstacle
        if (Input.GetKeyDown(KeyCode.F) && transformation == Transformation.FROG && obstacleToPull != null) {
            PullObject(obstacleToPull);
            EventDispatcher.Raise<StressAbility>(new StressAbility());
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

    void AnimationHandler() {

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

    // Bulldozer Actions

    public void SetBreakingTarget(GameObject target) {
        obstacleToBreak = target;
    }

    public GameObject GetBreakingTarget() {
        return obstacleToBreak;
    }

    // Frog Actions

    public void SetPullingTarget(GameObject target) {
        if (target == null) {
            if (obstacleToPull != null) {
                if (obstacleToPullColor != null) {
                    obstacleToPull.GetComponent<Renderer>().material.color = obstacleToPullColor;
                } else {
                    obstacleToPull.GetComponent<Renderer>().material.color = Color.white;
                }
            }
        }

        obstacleToPull = target;

        // set obstacle color to red
        if (obstacleToPull != null) {
            obstacleToPullColor = obstacleToPull.GetComponent<Renderer>().material.color;
            obstacleToPull.GetComponent<Renderer>().material.color = Color.red;
        }
    }

    public GameObject GetPullingTarget() {
        return obstacleToPull;
    }

    public void PullObject(GameObject target) {
        Debug.Log("Target: " + target);
        StartCoroutine(PullObjectCoroutine(target));
    }

    IEnumerator PullObjectCoroutine(GameObject target) {
        float timeElapsed = 0f;
        float duration = 1.5f;
        Vector3 originalPosition = target.transform.position;
        Vector3 targetPosition = transform.position;
        // if target position is below original position, match the y position
        if (targetPosition.y < originalPosition.y) {
            targetPosition.y = originalPosition.y;
        }

        while (timeElapsed < duration) {
            target.transform.position = Vector3.Lerp(originalPosition, targetPosition, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
    }

    public void SetHookingTarget(Transform target) {
        objectToHookTo = target;
    }

    public Transform GetHookingTarget() {
        return objectToHookTo;
    }

    void GrapplingHook(Transform objectToHookTo) {
        // Grappling Hook

        // Start Coroutine
        if (!usePhysicsHooking) StartCoroutine(GrapplingHookCoroutine(objectToHookTo));
        else StartCoroutine(GrapplingHookPhysicsCoroutine(objectToHookTo));
    }
    IEnumerator GrapplingHookCoroutine(Transform objectToHookTo) {
        // hook and move towards objectToHookTo
        float timeElapsed = 0f;
        float duration = hookDuration;
        Vector3 originalPosition = transform.position;
        Vector3 targetPosition = objectToHookTo.position;

        // if target position is below original position, match the y position
        if (targetPosition.y < originalPosition.y) {
            targetPosition.y = originalPosition.y;
        }

        while (timeElapsed < duration) {
            transform.position = Vector3.Lerp(originalPosition, targetPosition, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator GrapplingHookPhysicsCoroutine(Transform objectToHookTo) {
        // Loop the following until the object is close enough to the target or 3 seconds have passed

        float timeElapsed = 0f;

        while (Vector3.Distance(transform.position, objectToHookTo.position) > 1f && timeElapsed < hookDuration) {
            Vector3 direction = objectToHookTo.position - transform.position;
            rbody.AddForce(direction.normalized * hookForce, ForceMode.Impulse);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
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
    }
}
