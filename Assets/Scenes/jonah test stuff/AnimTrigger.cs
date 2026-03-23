using UnityEngine;

public class AnimTrigger : MonoBehaviour 
{
    [SerializeField] private Animator myAnimationController;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private string parameterName = "test";

    private void OnTriggerEnter(Collider other) 
    {
        if (other.CompareTag(targetTag)) 
        {
            myAnimationController.SetBool(parameterName, true);
        }
    }
    private void OnTriggerExit(Collider other) 
    {
        if (other.CompareTag(targetTag)) 
        {
            myAnimationController.SetBool(parameterName, false);
        }
    }

    /// <summary>
    /// Directly sets the animation parameter to true. Assign this to an
    /// Objective's On Restore Events list to replay the trigger on level reset.
    /// </summary>
    public void Trigger()
    {
        myAnimationController.SetBool(parameterName, true);
    }
}
