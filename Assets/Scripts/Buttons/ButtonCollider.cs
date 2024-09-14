using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonCollider : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag != "Player") return;
        else if (Player.Instance.GetTransformation() != Transformation.FROG) return;

        EventDispatcher.Raise<PressButton>(new PressButton());
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag != "Player") return;
        else if (Player.Instance.GetTransformation() != Transformation.FROG) return;
        
        EventDispatcher.Raise<ReleaseButton>(new ReleaseButton());
    }
}
