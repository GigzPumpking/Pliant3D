using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Terry : FormScript
{
    public float speed = 5.0f;
    public override void OnEnable()
    {
        if (Player.Instance != null)
        {
            Player.Instance.SetSpeed(speed);
        }
    }
}
