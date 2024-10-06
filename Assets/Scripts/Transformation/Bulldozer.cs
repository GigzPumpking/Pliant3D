using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bulldozer : FormScript
{
    public float speed = 3.0f;
    private int playerLayer = 3;
    private int walkableLayer = 7;

    public override void OnEnable()
    {
        PlayAudio("Bulldozer");
        Physics.IgnoreLayerCollision(playerLayer, walkableLayer, true);
        Player.Instance.SetSpeed(speed);
    }

    public void OnDisable()
    {
        Physics.IgnoreLayerCollision(playerLayer, walkableLayer, false);
    }
}
