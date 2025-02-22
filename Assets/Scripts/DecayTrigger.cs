using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecayTrigger : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float decayTime = 0.2f;

    private PhysicsObject physicsScript;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        physicsScript = GetComponent<PhysicsObject>();

        // Subscribe to the reset event.
        if (physicsScript != null)
        {
            physicsScript.OnResetEvent += HandleReset;
        }
    }

    // When the reset event is fired, set kinematic to true.
    private void HandleReset()
    {
        rb.isKinematic = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(UnfreezeYAxis());
        }
    }

    IEnumerator UnfreezeYAxis()
    {
        yield return new WaitForSeconds(decayTime);
        // Allow gravity to take effect by making the rigidbody non-kinematic.
        rb.isKinematic = false;
    }

    // Don't forget to unsubscribe when the object is destroyed!
    private void OnDestroy()
    {
        if (physicsScript != null)
        {
            physicsScript.OnResetEvent -= HandleReset;
        }
    }
}
