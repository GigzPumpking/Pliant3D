using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsometricCamera : MonoBehaviour
{
    public Transform followTarget;
    [SerializeField] private Vector3 targetPos;
    private float moveSpeed = 1.5f;
    public float xOffset = 0.0f;
    public float yOffset = 0.0f;
    public float zOffset = 0.0f;

    void Awake()
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
        if (followTarget == null && GameManager.Instance?.GetPlayer() != null) {
            followTarget = GameManager.Instance.GetPlayer();
        }

        if (followTarget == null) return;

        targetPos = new Vector3(followTarget.position.x + xOffset, followTarget.position.y + yOffset, followTarget.position.z + zOffset);
    }
}
