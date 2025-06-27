using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;    // Required for UI elements like Button
using UnityEngine.SceneManagement; // Required for scene management
using TMPro;             // If you are using TextMeshPro for button text

public class SceneSelectionPanelUI : MonoBehaviour
{
    public GameObject sceneButtonPrefab; // Assign a prefab for your scene selection button
    public Transform buttonContainer;    // Assign the parent transform where buttons will be instantiated (should have a Layout Group)

    public void PopulateSceneButtons(List<SceneSelectionTrigger.SceneEntry> scenes)
    {
        // Clear any existing buttons first. This is important regardless of how many new scenes there are.
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // --- Initial Checks ---
        if (sceneButtonPrefab == null || buttonContainer == null)
        {
            Debug.LogError("SceneButtonPrefab or ButtonContainer not assigned in SceneSelectionPanelUI.");
            gameObject.SetActive(false); // Ensure panel is hidden if setup is wrong
            return;
        }

        if (scenes == null || scenes.Count == 0)
        {
            Debug.LogWarning("No scenes provided to populate.");
            // Optionally, you could display a "No scenes available" message on a dedicated text element
            // or simply ensure the panel is hidden.
            gameObject.SetActive(false); // Ensure panel is hidden if no scenes
            return;
        }

        // --- Handle Single Scene Case ---
        if (scenes.Count == 1)
        {
            // If there's only one scene, directly trigger the action
            // without showing the panel or creating buttons.
            Debug.Log($"Only one scene offered ('{scenes[0].sceneName}'), automatically proceeding.");
            OnSceneButtonClicked(scenes[0].sceneToLoad);
            // OnSceneButtonClicked already calls gameObject.SetActive(false), so the panel will be hidden.
            return; // Skip button population and panel display
        }

        // --- Handle Multiple Scenes Case (populate buttons and show panel) ---
        // Make sure the panel is visible now that we know we need to show multiple buttons.
        gameObject.SetActive(true);

        foreach (var sceneEntry in scenes)
        {
            GameObject buttonGO = Instantiate(sceneButtonPrefab, buttonContainer);
            Button button = buttonGO.GetComponent<Button>();
            
            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>(); // For TextMeshPro
            // Text buttonText = buttonGO.GetComponentInChildren<Text>(); // For standard UI Text

            if (buttonText != null)
            {
                buttonText.text = sceneEntry.sceneName;
            }
            else
            {
                Debug.LogWarning("No Text/TextMeshProUGUI component found on the button prefab's children for: " + sceneEntry.sceneName);
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnSceneButtonClicked(sceneEntry.sceneToLoad));
            }
            else
            {
                Debug.LogError("Button component not found on the instantiated button prefab!");
            }
        }
    }

    void OnSceneButtonClicked(string sceneIdentifier)
    {
        NextScene.TargetScene = sceneIdentifier;

        // if current scene is the same as the target scene, don't do anything
        if (SceneManager.GetActiveScene().name == sceneIdentifier)
        {
            Debug.Log($"Current scene '{sceneIdentifier}' is the same as target scene. No action taken.");
            return;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.FadeIn();
        }
        else
        {
            Debug.LogError("UIManager.Instance is null. Cannot trigger FadeIn. Attempting direct scene load as fallback.");
            // Fallback or direct scene load if UIManager is critical and missing
            // Be cautious with direct load if fade is essential for game state.
            // NextScene.LoadSceneByNameOrIndex(sceneIdentifier); // You would need to make this method public static in NextScene.cs
        }

        // Deactivate the panel after selection or if auto-triggered
        gameObject.SetActive(false);
    }

    // Call this from a "Close" or "Cancel" button on your panel
    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}