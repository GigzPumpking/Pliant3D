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
        // Clear any existing buttons first.
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // --- Initial Checks ---
        if (sceneButtonPrefab == null || buttonContainer == null)
        {
            Debug.LogError("SceneButtonPrefab or ButtonContainer not assigned in SceneSelectionPanelUI.");
            gameObject.SetActive(false);
            return;
        }

        if (scenes == null || scenes.Count == 0)
        {
            Debug.LogWarning("No scenes provided to populate.");
            gameObject.SetActive(false);
            return;
        }

        // --- Handle Single Scene Case ---
        if (scenes.Count == 1)
        {
            Debug.Log($"Only one scene offered ('{scenes[0].sceneName}'), automatically proceeding.");
            OnSceneButtonClicked(scenes[0]); // Pass the whole entry
            return;
        }

        // --- Handle Multiple Scenes Case ---
        gameObject.SetActive(true);

        foreach (var sceneEntry in scenes)
        {
            GameObject buttonGO = Instantiate(sceneButtonPrefab, buttonContainer);
            Button button = buttonGO.GetComponent<Button>();
            
            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();

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
                // Pass the entire sceneEntry object to the handler
                button.onClick.AddListener(() => OnSceneButtonClicked(sceneEntry));
            }
            else
            {
                Debug.LogError("Button component not found on the instantiated button prefab!");
            }
        }
    }

    void OnSceneButtonClicked(SceneSelectionTrigger.SceneEntry sceneEntry)
    {
        // If current scene is the same as the target scene, don't do anything
        if (SceneManager.GetActiveScene().name == sceneEntry.sceneToLoad)
        {
            Debug.Log($"Current scene '{sceneEntry.sceneToLoad}' is the same as target scene. No action taken.");
            gameObject.SetActive(false); // Still hide the panel
            return;
        }

        if (sceneEntry.useLoadingScreen)
        {
            // Set up the multi-step transition via the NextScene script
            NextScene.SetupLoadingScreenTransition(
                sceneEntry.loadingScreenSceneName,
                sceneEntry.sceneToLoad,
                sceneEntry.loadingScreenDisplayTime
            );
        }
        else
        {
            // Set up a direct transition
            NextScene.TargetScene = sceneEntry.sceneToLoad;
        }

        // Trigger the initial fade-in, which will load the first target scene
        if (UIManager.Instance != null)
        {
            UIManager.Instance.FadeIn();
        }
        else
        {
            Debug.LogError("UIManager.Instance is null. Cannot trigger FadeIn. Attempting direct scene load as fallback.");
            SceneManager.LoadScene(NextScene.TargetScene);
        }

        // Deactivate the panel after selection
        gameObject.SetActive(false);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}