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
    private SpriteRenderer TerrySprite;
    private Transform sprite;

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
    [SerializeField] float movementSpeed = 1f;
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
        sprite = transform.Find("Sprite");
        animator = sprite.GetComponent<Animator>();
        TerrySprite = sprite.GetComponent<SpriteRenderer>();
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
        if (Input.GetKeyDown(KeyCode.T)) TransformationHandler();

        // if not touching the ground, set isGrounded to false
        if (rbody.velocity.y == 0) isGrounded = true;
        else isGrounded = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        MoveHandler();
        AnimationHandler();
    }

    void MoveHandler() {
        switch (transformation) {
            case(Transformation.TERRY):
                movementSpeed = 3f;
                break;
            case(Transformation.FROG):
                movementSpeed = 3.5f;
                break;
            case(Transformation.BULLDOZER):
                movementSpeed = 2f;
                break;
        }

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
        if (isGrounded) {
            Debug.Log("Jumping");
            if (direction == Direction.DOWN) animator.Play(jumpFrogDirections[0]);
            else animator.Play(jumpFrogDirections[2]);

            rbody.AddForce(new Vector3(0, 8, 0), ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void InputHandler() {
        float horizontal = 0f;
        float vertical = 0f;
        
        if (Input.GetKey(KeyCode.A)) {
            TerrySprite.flipX = false;
            horizontal = -1f;
            jumpDirection[1] = JumpDirection.LEFT;
        } else if (Input.GetKey(KeyCode.D)) {
            TerrySprite.flipX = true;
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

    void AnimationHandler() {
        switch(transformation) {
            case Transformation.TERRY:
                if (isMoving) {
                    if (direction == Direction.DOWN) {
                        animator.Play(runDirections[0]);
                    }
                    else {
                        animator.Play(runDirections[4]);
                    }
                }
                else {
                    if (direction == Direction.DOWN) {
                        animator.Play(staticDirections[0]);
                    }
                    else {
                        animator.Play(staticDirections[4]);
                    }
                }
                break;
            case Transformation.FROG:
                if (isMoving) {
                    if (direction == Direction.DOWN) {
                        animator.Play(jumpFrogDirections[1]);
                    }
                    else animator.Play(jumpFrogDirections[3]);
                } else {
                    if (direction == Direction.DOWN) {
                        animator.Play(staticFrogDirections[0]);
                    }
                    else animator.Play(staticFrogDirections[1]);
                }
                break;
            case Transformation.BULLDOZER:
                if (isMoving) {
                    if (direction == Direction.DOWN) {
                        animator.Play(walkBulldozerDirections[0]);
                    }
                    else animator.Play(walkBulldozerDirections[1]);
                } else {
                    if (direction == Direction.DOWN) animator.Play(staticBulldozerDirections[0]);
                    else animator.Play(staticBulldozerDirections[1]);
                }
                break;
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
}
