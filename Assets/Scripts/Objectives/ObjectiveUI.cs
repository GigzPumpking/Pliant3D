using System;
using TMPro;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

public class ObjectiveUI : MonoBehaviour {
    public Animator animator;
    public Image CheckBoxImage;
    public Image CheckMarkImage;
    public TextMeshProUGUI DescriptionTXT;
    public void OnComplete() {
        animator.SetBool("Complete", true);
        CheckMarkImage.gameObject.SetActive(true);
        GameManager.Instance?.AddQueuedTaskComplete();
    }

    /// <summary>
    /// Shows the completed visual (checkmark + animation) without incrementing the task counter.
    /// Used when restoring previously-completed objectives.
    /// </summary>
    public void SetCompletedVisual() {
        animator.SetBool("Complete", true);
        CheckMarkImage.gameObject.SetActive(true);
    }
}