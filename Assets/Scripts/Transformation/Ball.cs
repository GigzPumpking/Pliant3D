using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : FormScript
{
    public float speed = 7.0f;
    public override void OnEnable()
    {
        PlayAudio("Ball");
        Player.Instance.SetSpeed(speed);
    }
}