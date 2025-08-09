using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LockoutBar : MonoBehaviour
{
    private static LockoutBar instance;
    public static LockoutBar Instance { get { return instance; } }
    
    [SerializeField] private GameObject lockoutBar;
    [SerializeField] private int maxLockoutCharges = 4;
    
    internal class TransformationLOData
    {
        public LockoutBarUI LockoutBarUI;

        public int currentCharge
        {
            get => _currCharge;
            set
            {
                LockoutBarUI.SetCharge(value);
                _currCharge = value;
            }
        }

        private int _currCharge;
    }

    private Dictionary<Transformation, TransformationLOData> _lockoutTransformations = new Dictionary<Transformation, TransformationLOData>();
    private void Awake()
    {
        if (!instance) instance = this;
        else Destroy(this.gameObject);
    }

    private void Start()
    {
        InitializeTransformationData();
        InitializeTransformations();
    }

    private void InitializeTransformationData()
    {
        _lockoutTransformations.TryAdd(Transformation.TERRY, new TransformationLOData());
        _lockoutTransformations.TryAdd(Transformation.FROG, new TransformationLOData());
        _lockoutTransformations.TryAdd(Transformation.BULLDOZER, new TransformationLOData());
    }
    
    private void InitializeTransformations()
    {
        if (!_lockoutTransformations.Any()) return;
        foreach (TransformationLOData data in _lockoutTransformations.Values)
        {
            data.LockoutBarUI.CreateLockoutUI(maxLockoutCharges);
            data.currentCharge = maxLockoutCharges;
        }
    }
    
    public void AddCharge(Transformation transformation)
    {
        _lockoutTransformations[transformation].currentCharge++;
    }
    
    public void SubtractCharge(Transformation transformation)
    {
        _lockoutTransformations[transformation].currentCharge--;
    }
}