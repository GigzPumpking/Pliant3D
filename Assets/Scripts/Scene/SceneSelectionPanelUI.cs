using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;    // Required for UI elements like Button
using TMPro;             // If you are using TextMeshPro for button text

public class SceneSelectionPanelUI : MonoBehaviour
{
    public GameObject sceneButtonPrefab; // Assign a prefab for your scene selection button
    public Transform buttonContainer;    // Assign the parent transform where buttons will be instantiated

    // Optional: Ensure it's disabled by default if managed by UIManager but you want a Start() method.
    // void Start()
    // {
    //     gameObject.SetActive(false);
    // }

    public void PopulateSceneButtons(List<SceneSelectionTrigger.SceneEntry> scenes)
    {
        // Make sure the panel is visible before populating
        gameObject.SetActive(true);

        // Clear any existing buttons
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        if (sceneButtonPrefab == null || buttonContainer == null)
        {
            Debug.LogError("SceneButtonPrefab or ButtonContainer not assigned in SceneSelectionPanelUI.");
            gameObject.SetActive(false); // Hide if setup is wrong
            return;
        }

        if (scenes == null || scenes.Count == 0)
        {
            Debug.LogWarning("No scenes provided to populate.");
            // Optionally hide the panel if there are no scenes to show,
            // or show a message like "No destinations available."
            // gameObject.SetActive(false);
            // Add a text element to display "No scenes available"
            return;
        }

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
                // Ensure we are not adding listeners multiple times if buttons are reused/not cleared properly
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
        UIManager.Instance.FadeIn();

        // Deactivate the panel after selection
        gameObject.SetActive(false);
    }

    // Call this from a "Close" or "Cancel" button on your panel
    public void ClosePanel()
    {
        gameObject.SetActive(false);
        // No need to manage _currentPanelInstance in SceneSelectionTrigger anymore
    }
}