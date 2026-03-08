using System;
using UnityEngine;

public class UIConditionWindow : UIPopUpWindow
{
    public Action OnConditionWindowComplete;
    
    public void CompleteConditionWindow()
    {
        OnConditionWindowComplete?.Invoke();
    }
}