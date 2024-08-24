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

    enum JumpDirection {
        UP,
        DOWN,
        LEFT,
        RIGHT,
        NONE
    }
    
    private JumpDirection[] jumpDirection = new JumpDirection[2];

    bool isMoving = false;

    bool isGrounded = true;

    // Jumping and Movement Variables
    [SerializeField] float movementSpeed = 5f;
    Vector3 movement;
    float timeElapsed = 0f;
    public bool onRamp = false;
    public bool onPlatform = false;

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
    private Transform selectedGroup;

    // Other Variables

    public GameObject obstacleToPush;

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

        SetTransformation(Transformation.TERRY);

        smoke = transform.Find("Smoke");
        smokeAnimator = smoke.GetComponent<Animator>();
        sparkles = transform.Find("Sparkles");
        sparklesAnimator = sparkles.GetComponent<Animator>();
        sparkles.gameObject.SetActive(false);
        smoke.gameObject.SetActive(false);
        transformationBubble = transform.Find("Transformation Bubble");
    }

    public GameObject GetSmoke() {
        return smoke.gameObject;
    }

    void Update() {

        InputHandler();
        AnimationHandler();
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
        
        if (transformation == Transformation.BULLDOZER && obstacleToPush != null) {
            
        }

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

        rbody.MovePosition(transform.position + desiredMoveDirection * movementSpeed * Time.deltaTime);
    }

    void JumpHandler() {
        // if not touching the ground, set isGrounded to false
        if (rbody.velocity.y == 0) isGrounded = true;
        else isGrounded = false;

        if (isGrounded) {
            Debug.Log("Jumping");
            if (direction == Direction.DOWN) animator.Play("Jump Front");
            else animator.Play("Jump Back");

            rbody.AddForce(new Vector3(0, 8, 0), ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void InputHandler() {
        float horizontal = 0f;
        float vertical = 0f;
        
        if (Input.GetKey(KeyCode.A)) {
            selectedGroup.GetComponentInChildren<SpriteRenderer>().flipX = false;
            horizontal = -1f;
            jumpDirection[1] = JumpDirection.LEFT;
        } else if (Input.GetKey(KeyCode.D)) {
            selectedGroup.GetComponentInChildren<SpriteRenderer>().flipX = true;
            horizontal = 1f;
            jumpDirection[1] = JumpDirection.RIGHT;
        } else {
            jumpDirection[1] = JumpDirection.NONE;
        }

        if (Input.GetKey(KeyCode.W)) {
            vertical = 1f;
            direction = Direction.UP;
            jumpDirection[0] = JumpDirection.UP;
        } else if (Input.GetKey(KeyCode.S)) {
            vertical = -1f;
            direction = Direction.DOWN;
            jumpDirection[0] = JumpDirection.DOWN;
        } else {
            jumpDirection[0] = JumpDirection.NONE;
        }

        if (Input.GetKeyDown(KeyCode.Space) && transformation == Transformation.FROG) {
            JumpHandler();
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
                selectedGroup = terryGroup;
                animator = terryGroup.GetComponentInChildren<Animator>();
                break;
            case Transformation.FROG:
                terryGroup.gameObject.SetActive(false);
                frogGroup.gameObject.SetActive(true);
                bulldozerGroup.gameObject.SetActive(false);
                selectedGroup = frogGroup;
                animator = frogGroup.GetComponentInChildren<Animator>();
                break;
            case Transformation.BULLDOZER:
                terryGroup.gameObject.SetActive(false);
                frogGroup.gameObject.SetActive(false);
                bulldozerGroup.gameObject.SetActive(true);
                selectedGroup = bulldozerGroup;
                animator = bulldozerGroup.GetComponentInChildren<Animator>();
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

    public void SetPushingTarget(GameObject target) {
        obstacleToPush = target;
    }

    public GameObject GetPushingTarget() {
        return obstacleToPush;
    }
}
