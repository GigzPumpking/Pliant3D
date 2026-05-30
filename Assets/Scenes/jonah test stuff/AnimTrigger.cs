using UnityEngine;

public class AnimTrigger : MonoBehaviour 
{
    [SerializeField] private Animator myAnimationController;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private string parameterName = "test";

    [Header("Dependency")]
    [Tooltip("Optional. If assigned, this trigger will only fire after the referenced ButtonScript has been pressed.")]
    [SerializeField] private ButtonScript requiredButton;

    private bool IsActive => requiredButton == null || requiredButton.HasBeenTriggered;

    private void OnTriggerEnter(Collider other) 
    {
        if (!IsActive) return;

        if (other.CompareTag(targetTag)) 
        {
            myAnimationController.SetBool(parameterName, true);
        }
    }
    private void OnTriggerExit(Collider other) 
    {
        if (!IsActive) return;

        if (other.CompareTag(targetTag)) 
        {
            myAnimationController.SetBool(parameterName, false);
        }
    }

    public void Trigger()
    {
        if (!IsActive) return;

        myAnimationController.SetBool(parameterName, true);
    }
}
