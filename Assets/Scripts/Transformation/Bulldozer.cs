using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bulldozer : FormScript
{
    public float speed = 3.0f;
    private int playerLayer = 3;
    private int walkableLayer = 7;

    void Start()
    {
        EventDispatcher.AddListener<ShiftAbility>(PushState);

    }

    public override void OnEnable()
    {
        PlayAudio("Bulldozer");
        Player.Instance.SetSpeed(speed);
    }

    public void PushState(ShiftAbility e)
    {
        Physics.IgnoreLayerCollision(playerLayer, walkableLayer, e.isEnabled);
    }

    public void OnDisable()
    {
        Physics.IgnoreLayerCollision(playerLayer, walkableLayer, false);
    }

    public void OnDestroy()
    {
        EventDispatcher.RemoveListener<ShiftAbility>(PushState);
    }
}
