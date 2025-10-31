using System;
using UnityEngine;
using UnityEngine.UI;

public class EnterTutorialBox : MonoBehaviour
{
    public static event Action<bool, GameObject> OnEnter;
    [SerializeField] private GameObject tutorialObjectives;
    private Color _originalColor;
    
    void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        tutorialObjectives?.SetActive(true);
        OnEnter?.Invoke(true, gameObject);
    }
}
