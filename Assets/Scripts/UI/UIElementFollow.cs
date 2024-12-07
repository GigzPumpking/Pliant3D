using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIElementFollow : MonoBehaviour
{
    private Transform target; // The object that this label should follow
    private RectTransform rect; // The UI element RectTransform
    private Camera mainCamera; // The main camera

    [SerializeField] private Vector3 offset = new Vector3(0, 2, 0); // The offset from the target object

    void Start()
    {
        // Get the main camera
        mainCamera = Camera.main;

        // Get the UI element RectTransform
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        // If the target object is null, set it to the player
        if (target == null && Player.Instance != null)
        {
            target = Player.Instance.transform;
        }
    
        // Convert the player's world position to screen space
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(target.position + offset);
        
        // Update the bubble's anchored position
        rect.anchoredPosition = screenPosition - new Vector3(Screen.width / 2, Screen.height / 2, 0);
    }
}
