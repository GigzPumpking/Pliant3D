using System;
using UnityEngine;
using Unity.VisualScripting;

public class RechargeStation : MonoBehaviour {
    private TransformationWheel transformationWheel = null;
    public static event Action<float> OnRechargeStation;
    [SerializeField] private bool activateOnce;
    private bool _hasActivated = false;
    
    [SerializeField] private float rechargeAmt = 100f;
    
    [SerializeField] private Material usedMaterial;
    private Material _originalMaterial;

    [SerializeField] private MeshRenderer _meshRenderer;

    void Start()
    {
        StoreOriginalMaterial();
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (activateOnce && _hasActivated) return;

        if (activateOnce && !_hasActivated)
        {
            _meshRenderer.material = usedMaterial;
            _hasActivated = true;
        }

        //listened to by 'TransformationWheel.cs'
        OnRechargeStation?.Invoke(rechargeAmt);
    }

    void StoreOriginalMaterial()
    {
        if(!_meshRenderer) _meshRenderer = GetComponentInParent<MeshRenderer>();
        _originalMaterial = _meshRenderer.material;
    }
}