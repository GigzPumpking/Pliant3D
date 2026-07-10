using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadNextScene : MonoBehaviour
{
    private string sceneName;
    [SerializeField] private bool loadNextByDefault = true;

    [Header("Transition Settings")]
    [SerializeField] private Animator transitionAnimator;
    [SerializeField] private string fadeInStateName = "FadeIn";
    private string fadeInTrigger = "FadeIn";
    private string fadeOutTrigger = "FadeOut";

    public void Load(string sceneToLoad = null)
    {
        sceneName = sceneToLoad;

        Debug.Log($"LoadNextScene: Preparing to load scene '{sceneName ?? "next in build order"}'.");

        // Start the transition sequence instead of loading immediately
        StartCoroutine(TransitionToScene());
    }

    private IEnumerator TransitionToScene()
    {
        // 1. Play Fade In and wait until the FadedIn state is reached.
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger(fadeInTrigger);

            // Wait one frame for the Animator to process the trigger.
            yield return null;

            // Wait until the Animator enters the FadeIn state
            // (it may still be in FadedOut or a transition for a frame or two).
            while (!transitionAnimator.GetCurrentAnimatorStateInfo(0).IsName(fadeInStateName))
            {
                yield return null;
            }

            // Now wait until the Animator leaves the FadeIn state,
            // which means it has completed and moved on to FadedIn.
            while (transitionAnimator.GetCurrentAnimatorStateInfo(0).IsName(fadeInStateName))
            {
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning("No Animator assigned to LoadNextScene script.");
        }

        // 2. Begin Asynchronous Scene Load now that the screen is fully faded.
        AsyncOperation asyncLoad = null;

        if (!string.IsNullOrEmpty(sceneName))
        {
            Debug.Log($"LoadNextScene: Loading specified scene '{sceneName}'.");
            asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        }
        else if (loadNextByDefault)
        {
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                Debug.Log($"LoadNextScene: Loading next scene in build order with index {nextSceneIndex}.");
                asyncLoad = SceneManager.LoadSceneAsync(nextSceneIndex);
            }
            else
            {
                Debug.LogWarning("No next scene available in build settings.");
                yield break;
            }
        }
        else
        {
            Debug.LogWarning("No scene name specified and loadNextByDefault is false.");
            yield break;
        }

        // 3. Wait until the scene is fully loaded.
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 4. Play Fade Out to reveal the new scene.
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger(fadeOutTrigger);
        }
    }
}