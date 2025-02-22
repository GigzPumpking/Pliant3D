using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Ball : FormScript
{
    protected override float baseSpeed { get; set; } = 7.0f;
    [SerializeField] private float raycastDistanceDef = 1f;
    [SerializeField] private float yOffset = 0.5f;
    public float jumpForce = 6.0f;
    public float jumpForceDoubleJump = 3.0f;
    private bool isGrounded = true;
    private bool canDoubleJump = false;
    public float doubleJumpForgivenessTimeAfter  = 0.25f; //time in seconds the player can hit the space bar after hitting the ground to still doublejump
    public float doubleJumpForgivenessDistBefore = 0.5f; //distance in raycast the player can hit the space bar before hitting the ground to still doublejump
    int jumpCount = 0;

    public override void OnEnable()
    {
        base.OnEnable();
    }

    public override void Ability1(InputAction.CallbackContext context)
    {
        Debug.Log("Ball Ability 1");

        if (context.performed)
        {
            speed = baseSpeed * 2;
        } else if (context.canceled)
        {
            speed = baseSpeed;
        }
    }

    public override void Ability2(InputAction.CallbackContext context)
    {
        //Debug.Log("Ball using Frog Ability 1");
        if (!context.performed || jumpCount >= 1)
        {
            return;
        }

        if (!canDoubleJump)
        {
            jumpCount++;
            Vector3 rbDBJump = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.drag = 0;
            rb.useGravity = false;
            rb.velocity = rbDBJump;
            rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
            rb.useGravity = true;
            canDoubleJump = true;

            return;
        }

        if (!isGrounded) return;

        //has jumped, now is eligible to double jump
        isGrounded = false;

        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }

        if (!isGrounded && canDoubleJump)
        {
            rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
        }

        EventDispatcher.Raise<StressAbility>(new StressAbility());
    }

    float timeSinceJumped = 0f;
    private void Update()
    {
        if (canDoubleJump && !isGrounded)
        {
            timeSinceJumped += Time.deltaTime;
            if(timeSinceJumped > doubleJumpForgivenessTimeAfter)
            {
                canDoubleJump = false;
                timeSinceJumped = 0f;
            }
        }
    }

    private void GroundedChecker(float raycastDistance)
    {
        RaycastHit hit;

        //for regular jump
       isGrounded = Physics.Raycast(transform.position + new Vector3(0, yOffset, 0), Vector3.down * raycastDistance, out hit, raycastDistance);
        if (isGrounded)
        {
            //Debug.Log("Grounded");
            jumpCount = 0;
        }
        else {
            canDoubleJump = Physics.Raycast(transform.position + new Vector3(0, yOffset, 0),
                Vector3.down * raycastDistance, out hit, raycastDistance);
        }
    }

    private void CheckDoubleJump()
    {

    }

    private void FixedUpdate()
    {
        GroundedChecker(raycastDistanceDef);
    }
}