using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectionTrigger : MonoBehaviour
{
    Color originalColor;
    Color highlightColor = Color.red;

    void Start() {
        Debug.Log(this.transform.parent.gameObject.name);
        Player.Instance.SetBreakingTarget(null);
    }
    private void OnTriggerEnter(Collider other) {
        if (other.tag == "Player") {
            Player.Instance.SetBreakingTarget(this.transform.parent.gameObject);
            originalColor = this.transform.parent.gameObject.GetComponent<Renderer>().material.color;
            this.transform.parent.gameObject.GetComponent<Renderer>().material.color = highlightColor;
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.tag == "Player") {
            if (Player.Instance.GetBreakingTarget() == this.transform.parent.gameObject) {
                Player.Instance.SetBreakingTarget(null);
                if (originalColor != null) this.transform.parent.gameObject.GetComponent<Renderer>().material.color = originalColor;
            }
        }
    }
}
