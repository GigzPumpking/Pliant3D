using System;
using UnityEngine;
using Unity.VisualScripting;

public class RechargeStation : MonoBehaviour {
    private TransformationWheel transformationWheel = null;
    public static event Action OnRechargeStation;
    public bool activateOnce;
    private bool hasActivated = false;
    public SpriteRenderer spriteRenderer;
    
    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            if (activateOnce && hasActivated) return;

            if (activateOnce && !hasActivated)
                spriteRenderer.color = Color.gray;
            
            hasActivated = true;
            
            //listened to by 'TransformationWheel.cs'
            OnRechargeStation?.Invoke();
        }
    }
}