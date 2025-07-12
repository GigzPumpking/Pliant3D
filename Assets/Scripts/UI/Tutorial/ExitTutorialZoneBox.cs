using Unity;
using UnityEngine;
using UnityEngine.UI;

public class ExitTutorialZoneBox : MonoBehaviour
{
    public GameObject imgObject;
    private Color _originalColor;
    
    void Start()
    {
        imgObject?.SetActive(false);
    }
    void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        imgObject?.SetActive(true);
    }
    
    void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        imgObject?.SetActive(false);
    }
}
