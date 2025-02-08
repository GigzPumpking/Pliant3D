using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Ball : FormScript
{
    protected override float baseSpeed { get; set; } = 7.0f;

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
        
    }
}
