using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pushable : MonoBehaviour
{
    void Start() {
        // Add listener for ShiftAbility event
        EventDispatcher.AddListener<ShiftAbility>(PushState);
    }

    public void PushState(ShiftAbility e)
    {
        // if event's transformation is bulldozer and event is enabled, set kinematic to false
        if (e.transformation == Transformation.BULLDOZER && e.isEnabled)
        {
            GetComponent<Rigidbody>().isKinematic = false;
        } else {
            // if event's transformation is bulldozer and event is disabled, set kinematic to true
            GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    public void OnDestroy()
    {
        // Remove listener for ShiftAbility event
        EventDispatcher.RemoveListener<ShiftAbility>(PushState);
    }
}
