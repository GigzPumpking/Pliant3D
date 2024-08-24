using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonScript : MonoBehaviour
{
    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        animator.Play("Unpressed");
    }

    public void PressButton()
    {
        Debug.Log("Button pressed");
        animator.SetTrigger("Press");
    }

    public void UnpressButton()
    {
        Debug.Log("Button unpressed");
        animator.SetTrigger("Unpress");
    }
}
