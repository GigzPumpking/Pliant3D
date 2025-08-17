using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

//Handle all X amount of Lockout UI Charges for a SINGLE Transformation. Instantiated by LockoutBar.cs
public class LockoutBarUI : MonoBehaviour
{
    [SerializeField] private GameObject greenChargePrefab;
    [SerializeField] private GameObject yellowChargePrefab;
    [SerializeField] private GameObject orangeChargePrefab;
    [SerializeField] private GameObject redChargePrefab;

    private readonly Dictionary<Image, Image> _lockoutChargeImages = new Dictionary<Image, Image>(); //KEY: BG, VALUE: FILL
    private GameObject _lockoutTransformIcon;
    private GameObject _crossOutIcon;

    void Start()
    {
        this.gameObject.SetActive(false);
    }

    public void SetIcon(GameObject icon)
    {
        //HITS FIRST INSTANCE OF 'IMAGE' TYPE. WILL ALWAYS BE THE ICON
        _lockoutTransformIcon = GameObject.Instantiate(icon, this.gameObject.transform);
        _lockoutTransformIcon.name = "LockoutTransformIcon";
    }

    public void CrossOutIcon(GameObject icon)
    {
        _crossOutIcon = GameObject.Instantiate(icon, _lockoutTransformIcon.transform);
        _crossOutIcon.name = "CrossOutIcon";
        _crossOutIcon.AddComponent<LayoutElement>().ignoreLayout = true;
    }

    public void CreateLockoutUI(float maxCharges)
    {
        for (int i = 0; i < maxCharges; ++i)
        {
            GameObject go = null;
            float pos = ((i + 1) / maxCharges) * 100;
            go = pos switch
            {
                <= 25 => greenChargePrefab,
                > 25 and <= 50 => yellowChargePrefab,
                > 50 and <= 75 => orangeChargePrefab,
                > 75 and <= 100 => redChargePrefab,
                _ => greenChargePrefab
            };
            
            LockoutChargeUIFactory.CreateLockoutUI(go, this.transform, _lockoutChargeImages);
        }
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
