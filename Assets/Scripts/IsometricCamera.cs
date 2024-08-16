using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsometricCamera : MonoBehaviour
{
    public Transform followTarget;
    private Vector3 targetPos;
    public float moveSpeed = 1.0f;

    public float xOffset = 0.0f;
    public float yOffset = 0.0f;
    public float zOffset = 0.0f;

    void Start()
    {
        SetTargetPos();
    }

    void Update()
    {
        SetTargetPos();
        transform.position = Vector3.Lerp(transform.position, targetPos, moveSpeed * Time.deltaTime);
    }

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public void SetTargetPos() {
        targetPos = new Vector3(followTarget.position.x + xOffset, followTarget.position.y + yOffset, transform.position.z + zOffset);
    }
}
