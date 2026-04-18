using UnityEngine;
using UnityEngine.UI;

public class UIPopUpWindow : MonoBehaviour
{
    public GameObject defaultButton;
	[SerializeField] private AudioData onOpenSound;

    public void Awake()
    {
        //Try to find the default button if it wasn't assigned in the inspector
        //this will neccessitate null checks for this variable
        if (!defaultButton) GetComponentInChildren<Button>(true);
	
    }
    public void OnEnable()
    {
        UIStackedWindowTracker.Instance?.RegisterWindow(this);
		if (onOpenSound != null) {
			Debug.Log("Playing sound: " + "from" + this.gameObject.name);
			AudioManager.Instance.PlaySound(onOpenSound);
		}
        Debug.Log($"{gameObject.name}: was registered to the UIStackedWindowTracker");
    }

    public void OnDisable()
    {
        UIStackedWindowTracker.Instance?.UnregisterWindow(this);
        Debug.Log($"{gameObject.name}: was unregistered to the UIStackedWindowTracker");
    }

    //Current or last clicked button
    private GameObject _lastActivatedButton;
    
    public GameObject GetLastActivatedButton(){ return _lastActivatedButton; }
    public void SetLastActivatedButton(GameObject btn){ _lastActivatedButton = btn; }
}