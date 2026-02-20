using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonCollider : MonoBehaviour
{
    [Tooltip("The ButtonScript to activate. Auto-detected on this object or parent if left empty.")]
    [SerializeField] private ButtonScript buttonScript;

    private void Start()
    {
        if (buttonScript == null)
            buttonScript = GetComponentInParent<ButtonScript>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag != "Player") return;
        else if (Player.Instance.GetTransformation() != Transformation.FROG) return;

        if (buttonScript != null) buttonScript.Press();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag != "Player") return;
        else if (Player.Instance.GetTransformation() != Transformation.FROG) return;

        if (buttonScript != null) buttonScript.Release();
    }
}
