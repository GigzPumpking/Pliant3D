using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ButtonScript : MonoBehaviour
{
    private Animator animator;

    /// <summary>True once Press() has been called at least once.</summary>
    public bool HasBeenTriggered { get; private set; }

    void Start()
    {
        animator = GetComponent<Animator>();
        animator.Play("Unpressed");
    }

    public void Press()
    {
        if (animator == null) animator = GetComponent<Animator>();
        animator.SetTrigger("Press");
        HasBeenTriggered = true;
        OnPress();
    }

    public void Release()
    {
        if (animator == null) animator = GetComponent<Animator>();
        animator.SetTrigger("Unpress");
        OnRelease();
    }

    public abstract void OnPress();

    public abstract void OnRelease();
}
