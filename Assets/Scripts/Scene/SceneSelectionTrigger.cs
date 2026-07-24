using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Triggers a direct scene load, with an optional loading screen in between.
/// Can be activated by a physical trigger volume or by a direct call from a UI button.
/// </summary>
public class SceneSelectionTrigger : MonoBehaviour
{
    [System.Serializable]
    public class SceneEntry
    {
        [Tooltip("Display name (unused at runtime, for Inspector labelling only).")]
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
    [Tooltip("The target scene to load. Only the first entry is used.")]
    public List<SceneEntry> scenesToOffer = new List<SceneEntry>();

    [Header("Dependency")]
    [Tooltip("Optional. If assigned, this trigger will only work after the referenced ButtonScript has been pressed.")]
    [SerializeField] private ButtonScript requiredButton;

    private bool IsActive => requiredButton == null || requiredButton.HasBeenTriggered;
    private bool Collided = false;

    public void ActivatePanelFromButton()
    {
        if (!IsActive)
        {
            Debug.Log("ActivatePanelFromButton called but dependency not yet satisfied.");
            return;
        }
        LoadTargetScene();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsActive) return;
        if (Collided) return;

        if (other.CompareTag("Player"))
        {
            LoadTargetScene();
            Collided = true;
        }
    }

    private void LoadTargetScene()
    {
        if (scenesToOffer == null || scenesToOffer.Count == 0)
        {
            Debug.LogError("SceneSelectionTrigger: No scenes configured in scenesToOffer.");
            return;
        }

        SceneEntry entry = scenesToOffer[0];

        if (string.IsNullOrEmpty(entry.sceneToLoad))
        {
            Debug.LogError("SceneSelectionTrigger: sceneToLoad is empty.");
            return;
        }

        if (entry.useLoadingScreen)
        {
            NextScene.SetupLoadingScreenTransition(
                entry.loadingScreenSceneName,
                entry.sceneToLoad,
                entry.loadingScreenDisplayTime
            );
            if (UIManager.Instance != null)
            {
                UIManager.Instance.FadeIn();
            }
            else
            {
                Debug.LogError("SceneSelectionTrigger: UIManager.Instance is null. Cannot trigger FadeIn.");
            }
        }
        else
        {
            if (SceneLoader.Instance != null && SceneLoader.Instance.transition != null)
            {
                SceneLoader.Instance.LoadNextScene(entry.sceneToLoad);
            }
            else
            {
                Debug.LogWarning("SceneSelectionTrigger: SceneLoader is unavailable or its transition Animator is unassigned. Loading scene directly.");
                SceneManager.LoadScene(entry.sceneToLoad);
            }
        }
    }
}