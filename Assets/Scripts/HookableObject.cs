using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookableObject : MonoBehaviour
{
    public float range = 30f;

    private Color originalColor;
    [SerializeField] private Renderer[] renderers;

    void Start() {
        originalColor = GetComponent<Renderer>().material.color;
    }
    void Update() {

        if (Player.Instance.GetTransformation() == Transformation.FROG) {
            // Raycast towards the player if they are within range
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Player.Instance.transform.position - transform.position, out hit, range)) {
                if (hit.transform == Player.Instance.transform) {
                    Player.Instance.SetHookingTarget(transform);
                    // Change the color of all renderers to green
                    foreach (Renderer rend in renderers) {
                        rend.material.color = Color.green;
                    }

                } else {
                    if (Player.Instance.GetHookingTarget() == transform) {
                        Player.Instance.SetHookingTarget(null);
                        // Change the color of all renderers to original color
                        foreach (Renderer rend in renderers) {
                            rend.material.color = originalColor;
                        }
                    }
                }
            } else {
                Debug.DrawRay(transform.position, Player.Instance.transform.position - transform.position, Color.red);
                if (Player.Instance.GetHookingTarget() == transform) {
                    Player.Instance.SetHookingTarget(null);
                    // Change the color of all renderers to original color
                    foreach (Renderer rend in renderers) {
                        rend.material.color = originalColor;
                    }
                }
            }
        } else {
            foreach (Renderer rend in renderers) {
                rend.material.color = originalColor;
            }
        }

    }
}
