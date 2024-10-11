using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealDetection : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && Player.Instance.GetTransformation() == Transformation.TERRY)
        {
            Player.Instance.Heal();
        }
    }
}
