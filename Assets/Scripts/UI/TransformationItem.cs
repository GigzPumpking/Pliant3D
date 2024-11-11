using UnityEngine;

public class TransformationItem : MonoBehaviour
{

    private Animator animator;
    
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void HoverEnter()
    {
        animator.SetBool("Hover", true);
    }

    public void HoverExit()
    {
        animator.SetBool("Hover", false);
    }
}
