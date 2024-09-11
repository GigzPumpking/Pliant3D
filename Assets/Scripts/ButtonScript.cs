using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonScript : MonoBehaviour
{
    private Animator animator;
    public GameObject[] boxes;

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
        foreach(GameObject box in boxes){
            box.SetActive(false);
        }
    }

    public void UnpressButton()
    {
        Debug.Log("Button unpressed");
        animator.SetTrigger("Unpress");
    }
}
