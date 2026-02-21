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

    [SerializeField] private float resetThresholdY = -5f; // Example threshold for reset

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    // Resets the object's position and rotation, and notifies listeners.
    //cache rb if we need to reset velocity and angular velocity in the future
    private Rigidbody rb = null;
    public void ResetObject()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        TryGetComponent(out rb);
        if(rb) rb.velocity = Vector3.zero;
        
        

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

    // Reset the object if it goes falls below a certain Y threshold
    private void Update()
    {
        if (transform.position.y < resetThresholdY)
        {
            ResetObject();
        }
    }
}
