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
        Debug.LogWarning("Starting UI.");
    }

    public void SetIcon(GameObject icon)
    {
        //HITS FIRST INSTANCE OF 'IMAGE' TYPE. WILL ALWAYS BE THE ICON
        _lockoutTransformIcon = GameObject.Instantiate(icon, this.gameObject.transform);
        _lockoutTransformIcon.name = "LockoutTransformIcon";
    }

    public void CrossOutIcon(GameObject icon, bool isTerry = false)
    {
        if (!_crossOutIcon)
        {
            _crossOutIcon = GameObject.Instantiate(icon, _lockoutTransformIcon.transform.parent);
            if(isTerry) _crossOutIcon.transform.position = new Vector3(
                _lockoutTransformIcon.transform.position.x + _lockoutTransformIcon.GetComponent<RectTransform>().rect.width/2.5f,
                _lockoutTransformIcon.transform.position.y
            );
        }
        if(!isTerry) _crossOutIcon.transform.position = _lockoutTransformIcon.transform.position;
        _crossOutIcon.name = "CrossOutIcon";
        _crossOutIcon.AddComponent<LayoutElement>().ignoreLayout = true;
    }

    public void CrossOutIconActive(bool set) => _crossOutIcon?.SetActive(set);
    

    public void CreateLockoutUI(float maxCharges)
    {
        for (int i = 0; i < maxCharges; ++i)
        {
            GameObject go = null;
            float pos = ((i + 1) / maxCharges) * 100;
            go = pos switch
            {
                <= 25 => redChargePrefab,
                > 25 and <= 50 => orangeChargePrefab,
                > 50 and <= 75 => yellowChargePrefab,
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
            fill.fillAmount = idx < chargeAmt ? 100f : 0f;
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
