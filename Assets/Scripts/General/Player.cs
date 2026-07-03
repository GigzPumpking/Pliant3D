using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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
    
    // NEW: Public property to hold the direction vector calculated from the current animation state.
    public Vector3 AnimationBasedFacingDirection { get; private set; }

    // backing field for the above
    private Directions facingDirection = Directions.DOWN;  

    // track last pure horizontal or vertical intent
    private enum Axis { None, Horizontal, Vertical }
    private Axis lastDominantAxis = Axis.Horizontal;

    // to detect edges
    private bool prevH = false;
    private bool prevV = false;

    // track last non-zero horizontal so we know which way “left” refers to
    private Directions lastHorizontalInput = Directions.RIGHT;

    [SerializeField] bool isGrounded = true;
    public bool IsGrounded => isGrounded;

    private bool isJumping = false;
    public bool IsJumping => isJumping;

    // Grace window to prevent immediate upward clamp right after a jump/grapple impulse
    private float airborneGraceTimer = 0f;
    [SerializeField] private float airborneGraceDuration = 0.15f;

    // Jumping and Movement Variables
    [SerializeField] float movementSpeed = 5f;
    private Vector2 movementInput;
    
    // NEW: Stores the calculated direction from Update to be applied in FixedUpdate
    private Vector3 calculatedMoveDir = Vector3.zero;
    
    float timeElapsed = 0f;
    public bool canMove = true;

    // Transformation Variables
    public Transformation transformation = Transformation.TERRY;
    private Transform transformationWheel;
    [HideInInspector] public TransformationWheel transformationWheelScript;
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

    [SerializeField] private float minMoveThreshold = 0.25f;

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
            { "Ability3", (instance, ctx) => instance.Ability3Handler(ctx) },
            { "Unstick", (instance, ctx) => instance.UnstickHandler(ctx) }
        };

    protected override Dictionary<string, Action<Player, InputAction.CallbackContext>> KeyMapping => staticKeyMapping;

    protected override void OnEnable()
    {
        base.OnEnable();
        EventDispatcher.AddListener<StressDebuff>(StressDebuffHandler);
        EventDispatcher.AddListener<TogglePlayerMovement>(e => canMoveToggle(e.isEnabled));
        EventDispatcher.AddListener<NewSceneLoaded>(OnNewSceneLoaded);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventDispatcher.RemoveListener<StressDebuff>(StressDebuffHandler);
        EventDispatcher.RemoveListener<TogglePlayerMovement>(e => canMoveToggle(e.isEnabled));
        EventDispatcher.RemoveListener<NewSceneLoaded>(OnNewSceneLoaded);
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

        if (animator != null)
        {
            animator.SetBool("isWalking", false);

            // Default to down
            animator.SetFloat("MoveX", -1);
            animator.SetFloat("MoveY", -1);
        }

        EventDispatcher.Raise<DebugMessage>(new DebugMessage() { message = "Player Initialized" });
        
        selectedGroup.GetComponentInChildren<SpriteRenderer>().flipX = false;
    }

    public void stopMovement() {
        rbody.velocity = Vector3.zero;
        movementInput = Vector2.zero;
        calculatedMoveDir = Vector3.zero;

        animator?.SetBool("isWalking", false);
    }

    public GameObject GetSmoke() {
        return smoke.gameObject;
    }

    public void SetGroundedState(bool grounded)
    {
        // Ignore early ground hits while we are still within the airborne grace window
        if (grounded && airborneGraceTimer > 0f)
            return;

        isGrounded = grounded;

        // Landing ends jumping
        if (grounded)
        {
            isJumping = false;
            airborneGraceTimer = 0f;
        }
    }

    public void RegisterAirborneImpulse(float duration = -1f)
    {
        // If no custom duration provided, fall back to serialized default
        float dur = duration > 0f ? duration : airborneGraceDuration;
        airborneGraceTimer = Mathf.Max(airborneGraceTimer, dur);
    }

    public void SetJumpingState(bool jumping)
    {
        isJumping = jumping;
        if (jumping)
        {
            isGrounded = false;
        }
    }

    void InteractHandler(InputAction.CallbackContext context) {
        if (context.performed) {
            EventDispatcher.Raise<Interact>(new Interact());
            
            if (inObjectiveInteractable)
            {
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
        if (airborneGraceTimer > 0f)
        {
            airborneGraceTimer -= Time.deltaTime;
        }

        if (canMove)
        {
            InputHandler();
            CalculateMovement(); // Determines intent, applies sprite logic
        }
        else
        { 
            isMoving = false;
            calculatedMoveDir = Vector3.zero;
        }

        if (transform.position.y < outOfBoundsY && !GameManager.Instance.isGameOver
                                                && SceneManager.GetActiveScene().name != "2-0 Meri"
                                                && SceneManager.GetActiveScene().name != "3-0 Jerry"
                                                && SceneManager.GetActiveScene().name != "11-0 Thanks"
                                                && SceneManager.GetActiveScene().name != "0 Main Menu"
                                                && SceneManager.GetActiveScene().name != "11 End Screen"
                                                && SceneManager.GetActiveScene().name != "4-0 Carrie"
                                                && SceneManager.GetActiveScene().name != "5-0 Perry"
                                                && SceneManager.GetActiveScene().name != "11-0 End")
        {
            Debug.LogWarning("s::" + SceneManager.GetActiveScene().name);
            Debug.LogWarning("Game Over from Player.cs");
            GameManager.Instance?.GameOver();
        }

        UpdateAnimationBasedDirection();
    }

    // NEW: Apply the actual physics movement in FixedUpdate
    private void FixedUpdate()
    {
        if (!canMove) return;

        // Apply horizontal/depth velocity while maintaining current vertical velocity
        rbody.velocity = new Vector3(
            calculatedMoveDir.x * movementSpeed,
            rbody.velocity.y,
            calculatedMoveDir.z * movementSpeed
        );

        // Check if Bulldozer is sprinting - if so, don't clamp velocity
        bool isBulldozerSprinting = false;
        if (transformation == Transformation.BULLDOZER && selectedGroupScript != null)
        {
            Bulldozer bulldozer = selectedGroupScript as Bulldozer;
            if (bulldozer != null)
            {
                isBulldozerSprinting = bulldozer.IsSprinting();
            }
        }

        if (!isJumping && airborneGraceTimer <= 0f && isGrounded && rbody.velocity.y > 0.1f && !isBulldozerSprinting)
        {
            rbody.velocity = new Vector3(
                rbody.velocity.x,
                0,
                rbody.velocity.z
            );
        }
    }

    public void resetPosition() {
        transform.position = areaPositions[0];

        // Reset grounded/airborne state so the player isn't stuck as "not grounded"
        // after a respawn or scene reload.
        isGrounded = true;
        isJumping = false;
        airborneGraceTimer = 0f;
    }

    void setMovementInput(InputAction.CallbackContext context) {
        EventDispatcher.Raise<DebugMessage>(new DebugMessage() { message = $"Setting movement input: {context.ReadValue<Vector2>()}" });

        if (!isGrounded && transformation == Transformation.BULLDOZER) {
            movementInput = Vector2.zero;
            return;
        }
        
        // Prevent movement during Terry's fall animation
        if (transformation == Transformation.TERRY && animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("FrontFall_Terry"))
            {
                movementInput = Vector2.zero;
                return;
            }
        }

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

    // Renamed from MoveHandler. Calculates intent and updates sprites.
    void CalculateMovement() {
        float vx = movementInput.x;
        float vy = movementInput.y;
        float thr = minMoveThreshold;

        bool h = Mathf.Abs(vx) >= thr;
        bool v = Mathf.Abs(vy) >= thr;

        if (h && !prevH) lastDominantAxis = Axis.Horizontal;
        if (v && !prevV) lastDominantAxis = Axis.Vertical;

        prevH = h;
        prevV = v;
        
        // When the active form locks direction, skip facing/flip/animation updates
        bool directionLocked = selectedGroupScript != null && selectedGroupScript.IsDirectionLocked;

        if (!directionLocked)
        {
            if (h && lastDominantAxis == Axis.Horizontal)
            {
                facingDirection     = vx > 0 ? Directions.RIGHT : Directions.LEFT;
                lastHorizontalInput = facingDirection;
            }
            else if (v && lastDominantAxis == Axis.Vertical)
            {
                if (vy > 0)
                    facingDirection = (lastHorizontalInput == Directions.LEFT)
                                     ? Directions.UP
                                     : Directions.RIGHT;
                else
                    facingDirection = (lastHorizontalInput == Directions.LEFT)
                                     ? Directions.LEFT
                                     : Directions.DOWN;
            }

            if (h)
            {
                if (transformation == Transformation.BULLDOZER || transformation == Transformation.FROG)
                {
                    selectedGroup.GetComponentInChildren<SpriteRenderer>().flipX = (vx > 0);
                }
                else if (transformation == Transformation.TERRY)
                {
                    selectedGroup.GetComponentInChildren<SpriteRenderer>().flipX = false;
                }
            }
        }
        
        if (animator != null)
        {
            bool isMoving = vx != 0 || vy != 0;
            if (isMoving && !directionLocked)
            {
                animator.SetFloat("MoveX", vx);
                animator.SetFloat("MoveY", 3 * vy);
            }
            animator.SetBool("isWalking", vx != 0 || vy != 0);
        }
        
        Vector3 camF = Camera.main.transform.forward; camF.y = 0; camF.Normalize();
        Vector3 camR = Camera.main.transform.right;   camR.y = 0; camR.Normalize();
        Vector3 dir  = (camF * vy + camR * vx).normalized;

        // When direction is locked, only allow forward/backward along the locked facing
        if (directionLocked)
        {
            Vector3 lockedDir = AnimationBasedFacingDirection;
            lockedDir.y = 0f;
            if (lockedDir.sqrMagnitude > 0.001f)
            {
                lockedDir.Normalize();
                // Project input onto the locked axis; preserves sign (forward = push, backward = pull)
                dir = lockedDir * Vector3.Dot(dir, lockedDir);
            }
        }

        // Pass calculated direction to FixedUpdate
        calculatedMoveDir = dir;
    }
    
    private string GetCurrentSpriteName()
    {
        if (selectedGroupSprite == null)
        {
            return "";
        }

        return selectedGroupSprite.sprite.name;
    }

    private void UpdateAnimationBasedDirection()
    {
        if (selectedGroupScript != null && selectedGroupScript.IsDirectionLocked) return;
        if (selectedGroupSprite == null || animator == null) return;

        // NEW: Default to our EXISTING direction! 
        // If an animation (like Idle or Tongue) doesn't have a directional keyword in its name,
        // we just maintain the last known direction instead of breaking and snapping to forward.
        Vector3 dirVec = AnimationBasedFacingDirection == Vector3.zero ? Vector3.forward : AnimationBasedFacingDirection;

        bool isFlippedX = selectedGroupSprite.flipX;
        string spriteName = GetCurrentSpriteName();

        // Changed .StartsWith to .Contains to catch names like "Frog_Idle_FrontLeft"
        if (spriteName.Contains("FrontLeft"))
        {
            if (transformation == Transformation.BULLDOZER)
                dirVec = isFlippedX ? Vector3.back : Vector3.left;
            else
                dirVec = Vector3.left;
        }
        else if (spriteName.Contains("FrontRight"))
        {
            dirVec = Vector3.back;
        }
        else if (spriteName.Contains("BackLeft"))
        {
            if (transformation == Transformation.BULLDOZER)
                dirVec = isFlippedX ? Vector3.right : Vector3.forward;
            else
                dirVec = Vector3.forward;
        }
        else if (spriteName.Contains("BackRight"))
        {
            dirVec = Vector3.right;
        }
        else if (spriteName.Contains("Left")) // Pure Left/Right
        {
            if (transformation == Transformation.FROG)
                dirVec = isFlippedX ? new Vector3(1, 0, -1).normalized : new Vector3(-1, 0, 1).normalized;
            else
                dirVec = Vector3.left;
        }
        else if (spriteName.Contains("Back"))
        {
            dirVec = isFlippedX ? Vector3.right : Vector3.forward;
        }
        else if (spriteName.Contains("Front"))
        {
            dirVec = isFlippedX ? Vector3.back : Vector3.left;
        }

        AnimationBasedFacingDirection = dirVec;
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

    void UnstickHandler(InputAction.CallbackContext context) {
        if (UIManager.Instance && UIManager.Instance.isPaused) return;
        selectedGroup.GetComponent<FormScript>().Unstick(context);
    }

    void InputHandler() {
        for (int i = 1; i <= areaPositions.Length; i++) {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i)) {
                moveToArea(i - 1);
            }
        }
        
        if (InputManager.Instance && InputManager.Instance.isListening) {
            return;
        }
        
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

    public Transformation prevTransformation;
    public void SetTransformation(Transformation newTransformation)
    {
        if (transformation != newTransformation)
        {
            Smoke();
        }
        prevTransformation = transformation;
        transformation = newTransformation;

        foreach (var entry in transformationMapping.Values)
        {
            entry.group.gameObject.SetActive(false);
        }
        
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

    private void OnNewSceneLoaded(NewSceneLoaded e)
    {
        SetTransformation(Transformation.TERRY);

        // Clear any velocity accumulated during scenes with no floor (e.g. cutscene
        // transitions) so the player doesn't clip through geometry in the next level.
        if (rbody != null)
            rbody.velocity = Vector3.zero;

        isGrounded = true;
        isJumping = false;
        airborneGraceTimer = 0f;
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