using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Bulldozer : FormScript
{
    protected override float baseSpeed { get; set; } = 3.0f;
    private int playerLayer = 3;
    private int walkableLayer = 7;

    public override void OnEnable()
    {
        base.OnEnable();
        PlayAudio("Bulldozer");
    }

    public void PushState(bool state)
    {
        Debug.Log("PushState: " + state);

        Physics.IgnoreLayerCollision(playerLayer, walkableLayer, state);
        
        rb.mass = state ? 1000 : 1;
    }

    public void OnDisable()
    {
        Physics.IgnoreLayerCollision(playerLayer, walkableLayer, false);
    }

    public override void Ability1(InputAction.CallbackContext context)
    {
        Debug.Log("Bulldozer Ability 1");

        if (context.performed)
        {
            PushState(true);
        }
        else if (context.canceled)
        {
            PushState(false);
        }
    }

    public override void Ability2(InputAction.CallbackContext context)
    {
        
    }
}
