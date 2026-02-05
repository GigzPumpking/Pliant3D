using UnityEngine;

public class TriggerAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string animationName;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && animator != null && !string.IsNullOrEmpty(animationName))
        {
            animator.Play(animationName);
        }
    }
}
