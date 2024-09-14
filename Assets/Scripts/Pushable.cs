using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pushable : MonoBehaviour
{
    // Only let the player push the object

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // if Player is bulldozer
            if (Player.Instance.GetTransformation() == Transformation.BULLDOZER)
            {
                // Push the object
                Rigidbody2D rb = GetComponent<Rigidbody2D>();
                rb.velocity = collision.relativeVelocity;
            }
        }
    }
}
