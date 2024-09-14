using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ButtonScript : MonoBehaviour
{
    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        animator.Play("Unpressed");

        // Remove listeners to avoid duplicates
        EventDispatcher.RemoveListener<PressButton>(PressButton);
        EventDispatcher.RemoveListener<ReleaseButton>(ReleaseButton);

        EventDispatcher.AddListener<PressButton>(PressButton);
        EventDispatcher.AddListener<ReleaseButton>(ReleaseButton);
    }

    public void PressButton(PressButton e)
    {
        if (animator == null) animator = GetComponent<Animator>();
        animator.SetTrigger("Press");
        OnPress();
    }

    public void ReleaseButton(ReleaseButton e)
    {
        if (animator == null) animator = GetComponent<Animator>();
        animator.SetTrigger("Unpress");
        OnRelease();
    }

    public abstract void OnPress();

    public abstract void OnRelease();
}
