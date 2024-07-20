using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsometricCamera : MonoBehaviour
{
    public Transform followTarget;
    private Vector3 targetPos;
    public float moveSpeed = 1.0f;
}
