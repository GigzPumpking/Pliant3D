using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Object = System.Object;

public class LockoutBar : MonoBehaviour
{
    private static LockoutBar instance;
    public static LockoutBar Instance { get { return instance; } }
    
    [SerializeField] private int maxLockoutCharges = 4;
    
    [Header("Lockout UI Prefabs")]
    [SerializeField] private GameObject terryIcon;
    [SerializeField] private GameObject frogIcon;
    [SerializeField] private GameObject bulldozerIcon;
    [SerializeField] private GameObject crossoutIcon;
    [SerializeField] private GameObject lockoutBarPrefab;

    public Dictionary<Transformation, TransformationLOData> LockoutTransformations = new Dictionary<Transformation, TransformationLOData>();
    private void Awake()
    {
        if (!instance) instance = this;
        else Destroy(this.gameObject);
    }

    private void OnEnable()
    {
        TransformationWheel.OnTransform += SubtractCharge;
        PM_Meditation.OnMeditate += AddCharge;
    }

    private void OnDisable()
    {
        TransformationWheel.OnTransform -= SubtractCharge;
        PM_Meditation.OnMeditate -= AddCharge;
    }

    private void Start()
    {
        InitializeTransformationData();
        InitializeTransformations();
    }

    private TransformationLOData _terryData = new TransformationLOData();
    private TransformationLOData _frogData = new TransformationLOData();
    private TransformationLOData _bulldozerData = new TransformationLOData();
    private void InitializeTransformationData()
    {
        LockoutTransformations.TryAdd(Transformation.TERRY, _terryData);
        LockoutTransformations.TryAdd(Transformation.FROG, _frogData);
        LockoutTransformations.TryAdd(Transformation.BULLDOZER, _bulldozerData);
    }
    
    private void InitializeTransformations()
    {
        if (!LockoutTransformations.Any()) return;
        foreach (TransformationLOData data in LockoutTransformations.Values)
        {
            GameObject holder = GameObject.Instantiate(lockoutBarPrefab, this.transform); //HOLDER OBJECT
            holder.name = "Holder";

            LockoutBarUI currUI = holder.GetComponent<LockoutBarUI>();
            data.LockoutBarUI = currUI;
        }
        
        _terryData.LockoutBarUI.SetIcon(terryIcon);
        _frogData.LockoutBarUI.SetIcon(frogIcon);
        _bulldozerData.LockoutBarUI.SetIcon(bulldozerIcon);

        foreach (TransformationLOData data in LockoutTransformations.Values)
        {
            data.currentCharge = maxLockoutCharges;
            data.LockoutBarUI.CreateLockoutUI(maxLockoutCharges);
        }
    }
    
    //add lockout functionality
    //change UI based on form
    public void AddCharge(Transformation transformation)
    {
        SetCurrentLockoutBarActive(transformation);
        LockoutTransformations[transformation].currentCharge++;
    }
    
    public void SubtractCharge(Transformation transformation)
    {
        if(LockoutTransformations[transformation].isLockedOut) LockoutTransformations[transformation].LockoutBarUI?.CrossOutIcon(crossoutIcon);
        if (transformation == Player.Instance.transformation && transformation != Transformation.TERRY) return;
        SetCurrentLockoutBarActive(transformation);
        LockoutTransformations[transformation].currentCharge--;
    }

    public bool IsAnyLockedOut()
    {
        foreach (TransformationLOData data in LockoutTransformations.Values)
            if (data.currentCharge <= 0) return true;
        
        return false;
    }

    public void SetCurrentLockoutBarActive(Transformation transformation)
    {
        foreach (Transformation data in LockoutTransformations.Keys)
        {
            if (data == Transformation.TERRY && !IsAnyLockedOut()) continue;
            LockoutTransformations[data].LockoutBarUI.gameObject.SetActive(data == transformation);
        }
    }
}

[System.Serializable]
public class TransformationLOData
{
    public TransformationLOData(){}
    public TransformationLOData(int maxCharges){ currentCharge = maxCharges; }
    public LockoutBarUI LockoutBarUI = null;
    public bool isLockedOut => currentCharge <= 0;

    public int currentCharge
    {
        get => _currCharge;
        set
        {
            Debug.Log($"Current charge is {_currCharge}");
            LockoutBarUI.SetCharge(value);
            _currCharge = value;
        }
    }

    private int _currCharge;
    private bool _isLockedOut;
}