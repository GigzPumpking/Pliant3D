using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class NextScene : StateMachineBehaviour
{
    // --- STATIC STATE VARIABLES FOR TRANSITIONS ---

    // The immediate scene to load when OnStateExit is called.
    public static string TargetScene { get; set; } = null;

    // The name of the loading screen scene.
    private static string LoadingSceneName { get; set; } = null;

    // The final destination after the loading screen.
    private static string FinalDestinationScene { get; set; } = null;

    // How long to wait on the loading screen.
    private static float LoadingScreenDuration { get; set; } = 0f;

    // Flag to check if we are in a multi-step load.
    private static bool IsUsingLoadingScreen => !string.IsNullOrEmpty(FinalDestinationScene);


    // --- STATIC SETUP METHODS ---

    /// <summary>
    /// Configures the static variables for a transition that uses a loading screen.
    /// </summary>
    public static void SetupLoadingScreenTransition(string loadingScene, string finalScene, float duration)
    {
        LoadingSceneName = loadingScene;
        FinalDestinationScene = finalScene;
        LoadingScreenDuration = duration;
        TargetScene = LoadingSceneName; // The first step is to load the loading screen.
    }

    /// <summary>
    /// Resets all static transition variables to their default state.
    /// </summary>
    public static void ResetTransitionState()
    {
        TargetScene = null;
        LoadingSceneName = null;
        FinalDestinationScene = null;
        LoadingScreenDuration = 0f;
    }


    // --- STATE MACHINE BEHAVIOUR METHOD ---

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state.
    // This will now handle loading EITHER the loading screen OR the final scene.
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!string.IsNullOrEmpty(TargetScene))
        {
            Debug.Log($"Loading scene via StateExit: {TargetScene}");
            SceneManager.LoadScene(TargetScene);
            // The TargetScene is not reset here anymore. It's reset by the logic that sets up the next step.
        }
    }

    // --- SCENE LOAD EVENT HANDLING ---

    // Static constructor ensures we subscribe to the sceneLoaded event exactly once.
    static NextScene()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static bool TimedTransition = false;
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LockoutBar.Instance?.AddProgressToAllForms();
        // Check if we just loaded the loading screen as part of a multi-step transition.
        if (IsUsingLoadingScreen && scene.name == LoadingSceneName)
        {
            // We are on the loading screen. Create a temporary object to run a coroutine.
            var host = new GameObject("LoadingScreenWaitCoroutineHost").AddComponent<CoroutineHost>();
            if(TimedTransition) host.StartWaitSequence(LoadingScreenDuration);
        }
        else
        {
            // If we loaded any other scene (including the final destination),
            // ensure the transition state is clean for the next time.
            ResetTransitionState();
        }
    }
    
    public static void CallLoadNextScene()
    {
        NextScene.TargetScene = NextScene.FinalDestinationScene;
        SceneManager.LoadScene(NextScene.TargetScene);
    }

    // --- LIFECYCLE AND EDITOR HANDLING ---
    
    #if UNITY_EDITOR
    [UnityEditor.InitializeOnEnterPlayMode]
    static void OnEnterPlaymodeInEditor(UnityEditor.EnterPlayModeOptions options)
    {
        ResetTransitionState();
    }
    #endif

    // --- HELPER COMPONENT FOR COROUTINES ---

    /// <summary>
    /// A private MonoBehaviour helper class that can run Coroutines for us from a static context.
    /// An instance of this is created and destroyed automatically when needed.
    /// </summary>
    private class CoroutineHost : MonoBehaviour
    {
        public void StartWaitSequence(float duration)
        {
            StartCoroutine(WaitAndProceed(duration));
        }

        private IEnumerator WaitAndProceed(float duration)
        {
            Debug.Log($"On loading screen. Waiting for {duration} seconds.");
            yield return new WaitForSeconds(duration);

            Debug.Log("Wait finished. Preparing to load final scene.");

            // Set the next target to be the final destination.
            NextScene.TargetScene = NextScene.FinalDestinationScene;

            // Trigger the fade-out of the loading screen.
            if (UIManager.Instance != null)
            {
                UIManager.Instance.FadeIn();
            }
            else
            {
                Debug.LogError("UIManager instance not found! Cannot fade out of loading screen. Loading scene directly.");
                SceneManager.LoadScene(NextScene.TargetScene);
            }

            // The coroutine is done, so we destroy the host object.
            Destroy(gameObject);
        }
    }
}