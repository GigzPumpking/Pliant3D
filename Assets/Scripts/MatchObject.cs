using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchObject : MonoBehaviour
{
    // List of objects to match
    public List<GameObject> matchObjects;

    void OnTriggerEnter(Collider other)
    {
        if (matchObjects.Contains(other.gameObject))
        {
            Debug.Log("Matched!");

            other.gameObject.SetActive(false);
        }
    }
}
