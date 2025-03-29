using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class ObjectiveUI : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] TextMeshProUGUI description;

    void Start()
    {
        if (!TryGetComponent<Animator>(out animator)) Debug.LogWarning(gameObject.name + " has failed to retrieve an Animator Component");
        if (!TryGetComponent<TextMeshProUGUI>(out description)) Debug.LogWarning(gameObject.name + " has failed to retrieve an Description Component");
    }

    public void CompleteTask()
    {
        animator.SetBool("Complete", true);
    }

    public void SetDescription(string description)
    {
        this.description.text = description;
    }
}
