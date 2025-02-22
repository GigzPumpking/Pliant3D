using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Ball : FormScript
{
    protected override float baseSpeed { get; set; } = 7.0f;
    [SerializeField] private float raycastDistance = 1f;
    [SerializeField] private float yOffset = 0.5f;
    public float jumpForce = 6.0f;
    private bool isGrounded = true;

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
        Debug.Log("Ball using Frog Ability 1");

        if (!isGrounded || !context.performed)
        {
            return;
        }

        isGrounded = false;

        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }

        rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
        EventDispatcher.Raise<StressAbility>(new StressAbility());
    }

    private void GroundedChecker()
    {
        RaycastHit hit;
        isGrounded = Physics.Raycast(transform.position + new Vector3(0, yOffset, 0), Vector3.down * raycastDistance, out hit, raycastDistance);
    }

    private void CheckDoubleJump()
    {

    }

    private void FixedUpdate()
    {
        GroundedChecker();
    }
}
