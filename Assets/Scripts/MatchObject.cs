using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchObject : MonoBehaviour
{
    public GameObject matchObject;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == matchObject)
        {
            Debug.Log("Matched!");
            matchObject.SetActive(false);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject == matchObject)
        {
            Debug.Log("Unmatched!");
        }
    }
}
