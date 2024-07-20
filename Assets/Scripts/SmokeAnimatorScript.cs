using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeAnimatorScript : StateMachineBehaviour
{
    // When animation ends, set smoke to inactive

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        GameObject smoke = Player.Instance.GetSmoke();
        smoke.SetActive(false);
    }
}
