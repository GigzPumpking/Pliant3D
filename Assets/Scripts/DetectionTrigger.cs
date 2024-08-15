using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectionTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other) {
        if (other.tag == "Player") {
            Player.Instance.SetPushingTarget(this.transform.parent.gameObject);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.tag == "Player") {
            if (Player.Instance.GetPushingTarget() == this.transform.parent.gameObject) {
                Player.Instance.SetPushingTarget(null);
            }
        }
    }
}
