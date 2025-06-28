using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This component can trigger the display of a scene selection UI panel.
/// It can be activated either by a physical trigger volume in the scene (e.g., player walks into an area)
/// or by a direct call from a UI element like a button.
/// </summary>
public class SceneSelectionTrigger : MonoBehaviour
{
    [System.Serializable]
    public class SceneEntry
    {
        [Tooltip("The user-friendly name to display on the button.")]
        public string sceneName;

        [Tooltip("The exact name of the scene file to be loaded.")]
        public string sceneToLoad;

        [Header("Loading Screen Options")]
        [Tooltip("If checked, the transition will fade to a loading screen first.")]
        public bool useLoadingScreen;

        [Tooltip("The name of the loading scene to use. Required if 'Use Loading Screen' is checked.")]
        public string loadingScreenSceneName = "LoadingScreen";

        [Tooltip("The duration in seconds to wait on the loading screen.")]
        public float loadingScreenDisplayTime = 2.0f;
    }

    [Header("Scene Configuration")]
    [Tooltip("The list of scenes that will be presented as choices in the UI panel.")]
    public List<SceneEntry> scenesToOffer = new List<SceneEntry>();

    // Private cache for the UI component to avoid repeated calls to GetComponent.
    private SceneSelectionPanelUI _panelUIComponent;

    public void ActivatePanelFromButton()
    {
        Debug.Log("ActivatePanelFromButton called. Attempting to show scene selection panel.");
        ShowAndPopulatePanel();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered the trigger. Attempting to show scene selection panel.");
            ShowAndPopulatePanel();
        }
    }

    private void ShowAndPopulatePanel()
    {
        // First, check if the UIManager and its panel are accessible.
        if (UIManager.Instance == null || UIManager.Instance.returnScenePanel() == null)
        {
            Debug.LogError("UIManager instance or its sceneSelectionPanel is not available. Cannot show panel.");
            return; // Exit the function early if we can't proceed.
        }

        GameObject panelObject = UIManager.Instance.returnScenePanel();

        // Try to get the SceneSelectionPanelUI component if we haven't already.
        if (_panelUIComponent == null)
        {
            _panelUIComponent = panelObject.GetComponent<SceneSelectionPanelUI>();
        }

        // If the component is found, use it to populate the buttons.
        if (_panelUIComponent != null)
        {
            // This method is expected to handle setting the panel to active as well.
            _panelUIComponent.PopulateSceneButtons(scenesToOffer);
        }
        else
        {
            Debug.LogError("SceneSelectionPanelUI component not found on the UIManager's scene selection panel prefab!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Check if the panel is currently active before trying to deactivate it.
            if (UIManager.Instance != null && UIManager.Instance.returnScenePanel() != null && UIManager.Instance.returnScenePanel().activeSelf)
            {
                Debug.Log("Player exited the trigger. Hiding scene selection panel.");
                UIManager.Instance.returnScenePanel().SetActive(false);
            }
        }
    }
}