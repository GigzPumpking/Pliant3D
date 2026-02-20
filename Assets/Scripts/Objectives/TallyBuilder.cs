using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine;

public static class TallyBuilder
{
    public static void InitializeTallyUI(Objective obj, int total)
    {
        if(ObjectiveListing.ObjectiveToUI == null || !ObjectiveListing.ObjectiveToUI.ContainsKey(obj)) return;
        ObjectiveListing.ObjectiveToUI[obj].DescriptionTXT.text = $"{obj.description} (0/{total})";
    }
    
    public static void InitializeTallyUI(Objective obj, string total)
    {
        if(ObjectiveListing.ObjectiveToUI == null || !ObjectiveListing.ObjectiveToUI.ContainsKey(obj)) return;
        ObjectiveListing.ObjectiveToUI[obj].DescriptionTXT.text = $"{obj.description} (0/{total})";
    }
    
    public static void UpdateTallyUI(Objective obj, int current, int total)
    {
        if(ObjectiveListing.ObjectiveToUI == null || !ObjectiveListing.ObjectiveToUI.ContainsKey(obj)) return;
        ObjectiveListing.ObjectiveToUI[obj].DescriptionTXT.text = $"{obj.description} ({current}/{total})";
    }
}
