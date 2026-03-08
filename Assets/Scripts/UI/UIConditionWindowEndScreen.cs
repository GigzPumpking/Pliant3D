using System;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class UIConditionWindowEndScreen : MonoBehaviour
{
    public UIPopUpWindow PromotionScreen;
    public UIPopUpWindow FiredScreen;
    public TextMeshProUGUI ScoreText;
    public GameObject DesktopIcon;

    public void Start()
    {
        SetScoreText();
    }
    public void ShowScreen(UIPopUpWindow screen)
    {
        screen.gameObject.SetActive(true);
    }
    
    public void FindScreenToShow()
    {
        if (GameManager.GetRatioOfTasksCompleted() >= GameManager.Instance?.GetPromotionRatio())
        {
            ShowScreen(PromotionScreen);
        }
        else
        {
            ShowScreen(FiredScreen);
        }
    }

    public void SetScoreText()
    {
        if (!GameManager.Instance) return;
        ScoreText.text = (GameManager.GetRatioOfTasksCompleted() * 100) + "%";
    }
}
