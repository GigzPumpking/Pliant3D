using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsObject : MonoBehaviour
{
    // Obtains the location of the object within the scene when the game starts, and sets it as the initial position.
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    [SerializeField] private float resetYValue = -10f;

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    // Resets the object's position and rotation to the initial position and rotation.
    public void ResetObject()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }

    // Reset the object when its below a specific y value.
    public void ResetObjectBelowY(float yValue)
    {
        if (transform.position.y < yValue)
        {
            ResetObject();
        }
    }

    void Update()
    {
        // Reset the object when its below a specific y value.
        ResetObjectBelowY(resetYValue);
    }
}   
