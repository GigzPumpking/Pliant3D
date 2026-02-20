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
    }
}