using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneSelectionTrigger : MonoBehaviour
{
    [System.Serializable]
    public class SceneEntry
    {
        public string sceneName;   // The name to display on the button
        public string sceneToLoad; // The actual name of the scene file or its build index as a string
    }

    public List<SceneEntry> scenesToOffer = new List<SceneEntry>();
    private SceneSelectionPanelUI _panelUIComponent; // Cache the component

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (UIManager.Instance != null && UIManager.Instance.returnScenePanel() != null)
            {
                GameObject panelObject = UIManager.Instance.returnScenePanel();

                // Get the SceneSelectionPanelUI component if not already cached
                if (_panelUIComponent == null)
                {
                    _panelUIComponent = panelObject.GetComponent<SceneSelectionPanelUI>();
                }

                if (_panelUIComponent != null)
                {
                    _panelUIComponent.PopulateSceneButtons(scenesToOffer); // This will also set the panel to active
                    // panelObject.SetActive(true); // Ensure panel is active (PopulateSceneButtons should ideally handle this)
                }
                else
                {
                    Debug.LogError("SceneSelectionPanelUI component not found on the UIManager's sceneSelectionPanel!");
                }
            }
            else
            {
                Debug.LogError("UIManager instance or its sceneSelectionPanel is not available.");
            }
        }
    }

    // Optional: Deactivate panel if player leaves the trigger
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (UIManager.Instance != null && UIManager.Instance.returnScenePanel() != null && UIManager.Instance.returnScenePanel().activeSelf)
            {
                // You might want to check if this specific trigger was the one that opened it,
                // if multiple triggers could potentially control the same panel.
                // For simplicity, we assume any exit from a trigger closes it.
                UIManager.Instance.returnScenePanel().SetActive(false);
            }
        }
    }
}