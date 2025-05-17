using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetPlayer : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // if it's the player, reset the player
        if (other.CompareTag("Player"))
        {
            Player.Instance.resetPosition();
        }
    }
}
