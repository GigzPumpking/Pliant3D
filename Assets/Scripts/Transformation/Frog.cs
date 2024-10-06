using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Frog : FormScript
{
    public float speed = 6.0f;
    public override void OnEnable()
    {
        PlayAudio("Frog");
        Player.Instance.SetSpeed(speed);
    }
}
