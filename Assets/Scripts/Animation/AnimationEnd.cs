using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEnd : StateMachineBehaviour
{
    // When animation ends, set game object to inactive

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.gameObject.SetActive(false);
    }
}
