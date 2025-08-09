using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

//Handle all X amount of Lockout UI Charges for a SINGLE Transformation. Instantiated by LockoutBar.cs
public class LockoutBarUI : MonoBehaviour
{
    Dictionary<Image, Image> _lockoutChargeImages = new Dictionary<Image, Image>(); //KEY: BG, VALUE: FILL
    public GameObject lockoutTransformIcon;

    void Start()
    {
        this.gameObject.SetActive(false);
    }

    public void SetIcon(GameObject icon)
    {
        //HITS FIRST INSTANCE OF 'IMAGE' TYPE. WILL ALWAYS BE THE ICON
        lockoutTransformIcon = GameObject.Instantiate(icon, this.gameObject.transform);
        lockoutTransformIcon.name = "LockoutTransformIcon";
    }

    public void CreateLockoutUI(int maxCharges, GameObject lockoutChargePrefab)
    {
        for(int i = 0; i < maxCharges; ++i) LockoutChargeUIFactory.CreateLockoutUI(lockoutChargePrefab, this.transform, _lockoutChargeImages);
    }

    public void SetCharge(int chargeAmt)
    {
        int idx = 0;
        foreach (Image fill in _lockoutChargeImages.Values)
        {
            fill.fillAmount = idx <= chargeAmt ? 100f : 0f;
            idx++;
        }
    }

}

public class LockoutChargeUIFactory
{
    public static void SetLockoutBarIcon(GameObject go, Sprite icon) => go.GetComponent<Image>().sprite = icon;
    public static void CreateLockoutUI(GameObject prefab, Transform parent, Dictionary<Image,Image> refLockoutCharges)
    {
        Image bg = Object.Instantiate(prefab, parent).GetComponent<Image>();
        Image fill = bg.gameObject.transform.GetChild(0).GetComponent<Image>();
        refLockoutCharges.TryAdd(bg, fill);
    }
}
