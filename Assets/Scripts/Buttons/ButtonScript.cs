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

        EventDispatcher.AddListener<PressButton>(PressButton);
        EventDispatcher.AddListener<ReleaseButton>(ReleaseButton);
    }

    public void PressButton(PressButton e)
    {
        animator.SetTrigger("Press");
        OnPress();
    }

    public void ReleaseButton(ReleaseButton e)
    {
        animator.SetTrigger("Unpress");
        OnRelease();
    }

    public abstract void OnPress();

    public abstract void OnRelease();
}
