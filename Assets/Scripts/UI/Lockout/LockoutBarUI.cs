using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Object = UnityEngine.Object;

//Handle all X amount of Lockout UI Charges for a SINGLE Transformation. Instantiated by LockoutBar.cs
public class LockoutBarUI : MonoBehaviour
{
    [SerializeField] private List<Image> lockoutCharges = new List<Image>();
    [SerializeField] private GameObject lockoutChargePrefab;
    [SerializeField] private GameObject lockoutTransformIcon;

    public void CreateLockoutUI(int maxCharges)
    {
        for(int i = 0; i < maxCharges; ++i) LockoutChargeUIFactory.CreateLockoutUI(lockoutChargePrefab, this.transform, lockoutCharges);
    }
    
    public void SetCharge(int chargeAmt)
    {
        for (int i = 0; i < lockoutCharges.Count; ++i)
            lockoutCharges[i].fillAmount = i <= chargeAmt ? 100f : 0f;
    }

}

public class LockoutChargeUIFactory
{
    public static void CreateLockoutUI(GameObject prefab, Transform parent, List<Image> refLockoutCharges)
    {
        refLockoutCharges.Add(Object.Instantiate(prefab, parent).GetComponentInChildren<Image>());
    }
}
