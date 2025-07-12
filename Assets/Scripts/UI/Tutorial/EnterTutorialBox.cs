using UnityEngine;
public class EnterTutorialBox : MonoBehaviour
{
    [SerializeField] private GameObject tutorialObjectives;
    void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        tutorialObjectives?.SetActive(true);
    }
}
