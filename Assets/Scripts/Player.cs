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

    enum Direction {
        UP,
        DOWN
    }
    private Direction direction = Direction.DOWN;

    bool isMoving = false;

    enum LastInput {
        UP,
        DOWN,
        LEFT,
        RIGHT
    }

    private LastInput lastInput = LastInput.DOWN;

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

    // Other Variables
    public GameObject obstacleToBreak;
    public GameObject obstacleToPull;
    public Color obstacleToPullColor;

    [SerializeField] private float raycastDistance = 1f;
    [SerializeField] private float yOffset = 0.5f;

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
    }

    public GameObject GetSmoke() {
        return smoke.gameObject;
    }

    void Update() {

        InputHandler();
        AnimationHandler();
        GroundedChecker();
        if (Input.GetKeyDown(KeyCode.T)) TransformationHandler();
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
        
        MoveHandler();
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

        Vector3 desiredMoveDirection = cameraForwards * verticalInput + cameraRight * horizontalInput;

        if (desiredMoveDirection != Vector3.zero) {
            isMoving = true;
        } else {
            isMoving = false;
        }

        rbody.velocity = new Vector3(desiredMoveDirection.x * movementSpeed, rbody.velocity.y, desiredMoveDirection.z * movementSpeed);
    }

    void JumpHandler() {
        if (isGrounded) {
            if (direction == Direction.DOWN) animator.Play("Jump Front");
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
            case LastInput.UP:
                facingDirection = transform.TransformDirection(Vector3.forward);
                break;
            case LastInput.DOWN:
                facingDirection = transform.TransformDirection(Vector3.back);
                break;
            case LastInput.LEFT:
                facingDirection = transform.TransformDirection(Vector3.left);
                break;
            case LastInput.RIGHT:
                facingDirection = transform.TransformDirection(Vector3.right);
                break;
        }

        if (Physics.Raycast(position, facingDirection, out hit, 10f, layerMask)) {
            Debug.DrawRay(position, facingDirection * hit.distance, Color.red);
            if (hit.collider.gameObject != obstacleToPull && transformation == Transformation.FROG) {
                SetPullingTarget(hit.collider.gameObject);
            }
        } else {
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
            lastInput = LastInput.LEFT;
        } else if (Input.GetKey(KeyCode.D)) {
            selectedGroup.GetComponentInChildren<SpriteRenderer>().flipX = true;
            horizontal = 1f;
            lastInput = LastInput.RIGHT;
        }

        if (Input.GetKey(KeyCode.W)) {
            vertical = 1f;
            direction = Direction.UP;
            lastInput = LastInput.UP;
        } else if (Input.GetKey(KeyCode.S)) {
            vertical = -1f;
            direction = Direction.DOWN;
            lastInput = LastInput.DOWN;
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

    void AnimationHandler() {
        if (isMoving) {
            if (direction == Direction.DOWN) {
                animator.Play("Walk Front");
            }
            else {
                animator.Play("Walk Back");
            }
        }
        else {
            if (direction == Direction.DOWN) {
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
}
