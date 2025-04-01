using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsObject : MonoBehaviour
{
    // Event to notify subscribers when the object is reset.
    public event Action OnResetEvent;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    // Resets the object's position and rotation, and notifies listeners.
    public void ResetObject()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // Notify subscribers that a reset occurred.
        OnResetEvent?.Invoke();
    }

    // Reset the object if it collides with an object tagged "Floor"
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
        {
            ResetObject();
        }
    }
}
