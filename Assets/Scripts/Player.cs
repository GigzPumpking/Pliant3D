using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
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

    [SerializeField] float baseSpeed = 5f;
    [SerializeField] float movementSpeed = 5f;
    Vector3 movement;
    float timeElapsed = 0f;
    public float jumpForce = 8f;
    public bool canMove = true;

    public static readonly string[] staticDirections = { "Idle Front", "Hurt Idle Front 1", "Hurt Idle Front 2", "Hurt Idle Front 3", "Idle Back", "Hurt Idle Back 1", "Hurt Idle Back 2", "Hurt Idle Back 3"};
    public static readonly string[] staticFrogDirections = { "Idle Front Frog", "Idle Back Frog"};
    public static readonly string[] jumpFrogDirections = { "Jump Front Frog", "Walk Front Frog", "Jump Back Frog", "Walk Back Frog"};
    public static readonly string[] staticBulldozerDirections = { "Idle Front Bulldozer", "Idle Back Bulldozer"};
    public static readonly string[] walkBulldozerDirections = { "Walk Front Bulldozer", "Walk Back Bulldozer"};
    public static readonly string[] runDirections = {"Walk Front", "Hurt Walk Front 1", "Hurt Walk Front 2", "Hurt Walk Front 3", "Walk Back", "Hurt Walk Back 1", "Hurt Walk Back 2", "Hurt Walk Back 3"};

    // Transformation Variables
    public Transformation transformation = Transformation.TERRY;
    private Transform transformationBubble;
    private Transform smoke;
    private Transform shadow;
    private Transform terryGroup;
    private Transform frogGroup;
    private Transform bulldozerGroup;
    private Transform ballGroup;
    private Transform selectedGroup;

    public float transformationDuration = 10f;

    // Other Variables
    public GameObject obstacleToBreak;
    public GameObject obstacleToPull;
    public Transform objectToHookTo;
    public float hookForce = 0.5f;
    public float hookDuration = 3f;
    public bool usePhysicsHooking = false;
    public Color obstacleToPullColor;

    [SerializeField] private float raycastDistance = 1f;
    [SerializeField] private float yOffset = 0.5f;

    [SerializeField] private Vector3 area1Position = new Vector3(-14.63f, 0.89f, 0.83f);

    [SerializeField] private Vector3 area2Position = new Vector3(8.93f, 0.89f, 69.6f);

    [SerializeField] private Vector3 area3Position = new Vector3(64.8f, 0.89f, 33f);

    [SerializeField] private Vector3 area4Position = new Vector3(88.29f, 0.89f, -18f);

    public bool directionalMovement = false;
    void Start()
    {
        EventDispatcher.AddListener<StressDebuff>(StressDebuffHandler);
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            // move correct instance to this instance's location before destroying
            instance.transform.position = this.transform.position;
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        rbody = GetComponent<Rigidbody>();

        terryGroup = transform.Find("Terry");
        frogGroup = transform.Find("Frog");
        bulldozerGroup = transform.Find("Bulldozer");
        ballGroup = transform.Find("Ball");

        SetTransformation(Transformation.TERRY);

        smoke = transform.Find("Smoke");
        smokeAnimator = smoke.GetComponent<Animator>();
        sparkles = transform.Find("Sparkles");
        sparklesAnimator = sparkles.GetComponent<Animator>();
        sparkles.gameObject.SetActive(false);
        smoke.gameObject.SetActive(false);
        transformationBubble = transform.Find("Transformation Bubble");

        movementSpeed = baseSpeed;

        Debug.Log("Area 1 Position: " + area1Position);
        Debug.Log("Area 2 Position: " + area2Position);
        Debug.Log("Area 3 Position: " + area3Position);
        Debug.Log("Area 4 Position: " + area4Position);
    }

    public GameObject GetSmoke() {
        return smoke.gameObject;
    }

    void Update() {

        InputHandler();
        AnimationHandler();
        GroundedChecker();
        if (Input.GetKeyDown(KeyCode.T)) TransformationHandler();

        if (Input.GetKeyDown(KeyCode.G) && transformation == Transformation.FROG) {
            if (objectToHookTo != null) GrapplingHook(objectToHookTo);
        }

        // if E is pressed, interact with object
        if (Input.GetKeyDown(KeyCode.E)) {
            EventDispatcher.Raise<Interact>(new Interact());
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (transformation == Transformation.BULLDOZER) {
            rbody.mass = 1000;
        } else {
            rbody.mass = 1;
        }

        PullChecker();

        if (!transformationBubble.gameObject.activeSelf) {
            MoveHandler();
        }
    }

    void MoveHandler() {
        float verticalInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");

        Vector3 cameraForwards = Camera.main.transform.forward;

        Vector3 cameraRight = Camera.main.transform.right;

        cameraForwards.y = 0f;
        cameraRight.y = 0f;
        cameraForwards = cameraForwards.normalized;
        cameraRight = cameraRight.normalized;

        // if vertical input or horizontal input are 0, set them to the lastVerticalInput or lastHorizontalInput
        // unless both are 0, then it's not moving

        if (directionalMovement) {
            if (verticalInput != 0 || horizontalInput != 0) {
                if (verticalInput == 0) {
                    verticalInput = (lastVerticalInput == Directions.UP) ? 1 : -1;
                }
                if (horizontalInput == 0) {
                    horizontalInput = (lastHorizontalInput == Directions.RIGHT) ? 1 : -1;
                }
            }   
        }

        Vector3 desiredMoveDirection = cameraForwards * verticalInput + cameraRight * horizontalInput;

        // if there is Vertical Input and Horizontal Input

        if (desiredMoveDirection != Vector3.zero) {
            isMoving = true;
        } else {
            isMoving = false;
        }

        rbody.velocity = new Vector3(desiredMoveDirection.x * movementSpeed, rbody.velocity.y, desiredMoveDirection.z * movementSpeed);
    }

    public void SetVelocity(Vector3 velocity) {
        rbody.velocity = velocity;
    }

    public void SetSpeed(float speed) {
        baseSpeed = speed;
    }

    void JumpHandler() {
        if (isGrounded) {
            if (lastVerticalInput == Directions.DOWN) animator.Play("Jump Front");
            else animator.Play("Jump Back");

            rbody.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void GroundedChecker() {
        RaycastHit hit;
        // Cast from transform.position + Y offset of 0.5f
        Debug.DrawRay(transform.position + new Vector3(0, 0.5f, 0), Vector3.down * raycastDistance, Color.green);
        if (Physics.Raycast(transform.position + new Vector3(0, yOffset, 0), Vector3.down * raycastDistance, out hit, raycastDistance)) {
            isGrounded = true;
        } else {
            isGrounded = false;
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
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.LeftShift) && transformation == Transformation.BALL) {
            movementSpeed = baseSpeed * 2;
        } else {
            movementSpeed = baseSpeed;
        }
        
        if (Input.GetKey(KeyCode.A)) {
            selectedGroup.GetComponentInChildren<SpriteRenderer>().flipX = false;
            horizontal = -1f;
            lastHorizontalInput = Directions.LEFT;
            lastInput = Directions.LEFT;
        } else if (Input.GetKey(KeyCode.D)) {
            selectedGroup.GetComponentInChildren<SpriteRenderer>().flipX = true;
            horizontal = 1f;
            lastHorizontalInput = Directions.RIGHT;
            lastInput = Directions.RIGHT;
        }

        if (Input.GetKey(KeyCode.W)) {
            vertical = 1f;
            lastVerticalInput = Directions.UP;
            lastInput = Directions.UP;
        } else if (Input.GetKey(KeyCode.S)) {
            vertical = -1f;
            lastVerticalInput = Directions.DOWN;
            lastInput = Directions.DOWN;
        }

        if (Input.GetKeyDown(KeyCode.Space) && transformation == Transformation.FROG) {
            JumpHandler();
        }

        // if F is pressed while in Bulldozer form, break the obstacle
        if (Input.GetKeyDown(KeyCode.F) && transformation == Transformation.BULLDOZER && obstacleToBreak != null) {
            obstacleToBreak.SetActive(false);
            obstacleToBreak = null;
        }

        // if F is pressed while in Frog form, pull the obstacle
        if (Input.GetKeyDown(KeyCode.F) && transformation == Transformation.FROG && obstacleToPull != null) {
            PullObject(obstacleToPull);
        }

        // if 1 is pressed, move to area 1
        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            Area1();
        }

        // if 2 is pressed, move to area 2
        if (Input.GetKeyDown(KeyCode.Alpha2)) {
            Area2();
        }

        // if 3 is pressed, move to area 3
        if (Input.GetKeyDown(KeyCode.Alpha3)) {
            Area3();
        }

        // if 4 is pressed, move to area 4
        if (Input.GetKeyDown(KeyCode.Alpha4)) {
            Area4();
        }
    }

    public void TransformationHandler() {
        if (!transformationBubble.gameObject.activeSelf) {
            transformationBubble.gameObject.SetActive(true);
        }
    }

    public bool TransformationChecker() {
        return (transformationBubble.gameObject.activeSelf);
    }

    public void SetTransformation(Transformation newTransformation) {
        if (transformation != newTransformation) {
            Smoke();
        }
        transformation = newTransformation;

        switch(transformation) {
            case Transformation.TERRY:
                terryGroup.gameObject.SetActive(true);
                frogGroup.gameObject.SetActive(false);
                bulldozerGroup.gameObject.SetActive(false);
                ballGroup.gameObject.SetActive(false);
                selectedGroup = terryGroup;
                animator = terryGroup.GetComponentInChildren<Animator>();
                break;
            case Transformation.FROG:
                terryGroup.gameObject.SetActive(false);
                frogGroup.gameObject.SetActive(true);
                bulldozerGroup.gameObject.SetActive(false);
                ballGroup.gameObject.SetActive(false);
                selectedGroup = frogGroup;
                animator = frogGroup.GetComponentInChildren<Animator>();
                break;
            case Transformation.BULLDOZER:
                terryGroup.gameObject.SetActive(false);
                frogGroup.gameObject.SetActive(false);
                bulldozerGroup.gameObject.SetActive(true);
                ballGroup.gameObject.SetActive(false);
                selectedGroup = bulldozerGroup;
                animator = bulldozerGroup.GetComponentInChildren<Animator>();
                break;
            case Transformation.BALL:
                terryGroup.gameObject.SetActive(false);
                frogGroup.gameObject.SetActive(false);
                bulldozerGroup.gameObject.SetActive(false);
                ballGroup.gameObject.SetActive(true);
                selectedGroup = ballGroup;
                animator = ballGroup.GetComponentInChildren<Animator>();
                break;
            default:
            // Default to Terry
                terryGroup.gameObject.SetActive(true);
                frogGroup.gameObject.SetActive(false);
                bulldozerGroup.gameObject.SetActive(false);
                selectedGroup = terryGroup;
                animator = terryGroup.GetComponentInChildren<Animator>();
                break;
        }
    }
    public Transformation GetTransformation() {
        return transformation;
    }

    public void StressDebuffHandler(StressDebuff e) {
        SetTransformation(Transformation.TERRY);
    }

    void AnimationHandler() {
        // if animator is null, return
        if (animator == null) return;

        if (isMoving) {
            if (lastVerticalInput == Directions.DOWN) {
                animator.Play("Walk Front");
            }
            else {
                animator.Play("Walk Back");
            }
        }
        else {
            if (lastVerticalInput == Directions.DOWN) {
                animator.Play("Idle Front");
            }
            else {
                animator.Play("Idle Back");
            }
        }
    }

    public void Smoke() {
        smoke.gameObject.SetActive(true);
        smokeAnimator.Play("Smoke");
    }

    public void HealAnim() {
        sparkles.gameObject.SetActive(true);
        sparklesAnimator.Play("Heal Anim");
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

    public void Area1() {
        Debug.Log("Area 1 Position: " + area1Position);
        transform.position = area1Position;
    }

    public void Area2() {
        Debug.Log("Area 2 Position: " + area2Position);
        transform.position = area2Position;
    }

    public void Area3() {
        transform.position = area3Position;
    }

    public void Area4() {
        transform.position = area4Position;
    }

    public void ToggleMovement() {
        directionalMovement = !directionalMovement;
    }
}
