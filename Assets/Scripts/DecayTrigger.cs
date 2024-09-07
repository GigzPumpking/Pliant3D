using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecayTrigger : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float decayTime = 0.2f;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) {
            StartCoroutine(UnfreezeYAxis());
        }
    }

    IEnumerator UnfreezeYAxis()
    {
        yield return new WaitForSeconds(decayTime);
        // set kinematic to false to allow gravity to take effect
        rb.isKinematic = false;
    }
}
