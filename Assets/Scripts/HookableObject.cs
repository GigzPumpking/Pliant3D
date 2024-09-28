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
            if (Physics.Raycast(transform.position, GameManager.Instance.player.position - transform.position, out hit, range)) {
                if (hit.transform == GameManager.Instance.player) {
                    Debug.DrawRay(transform.position, GameManager.Instance.player.position - transform.position, Color.green);
                    Player.Instance.SetHookingTarget(transform);
                    // Change the color of all renderers to green
                    foreach (Renderer rend in renderers) {
                        rend.material.color = Color.green;
                    }

                } else {
                    Debug.DrawRay(transform.position, GameManager.Instance.player.position - transform.position, Color.red);
                    if (Player.Instance.GetHookingTarget() == transform) {
                        Player.Instance.SetHookingTarget(null);
                        // Change the color of all renderers to original color
                        foreach (Renderer rend in renderers) {
                            rend.material.color = originalColor;
                        }
                    }
                }
            } else {
                Debug.DrawRay(transform.position, GameManager.Instance.player.position - transform.position, Color.red);
                if (Player.Instance.GetHookingTarget() == transform) {
                    Player.Instance.SetHookingTarget(null);
                    // Change the color of all renderers to original color
                    foreach (Renderer rend in renderers) {
                        rend.material.color = originalColor;
                    }
                }
            }
        }

    }
}
