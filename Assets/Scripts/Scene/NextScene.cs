using UnityEngine;
using UnityEngine.SceneManagement;

public class NextScene : StateMachineBehaviour
{
    // Static variable to hold the name or build index of the scene to load.
    // This can be set from anywhere, e.g., your SceneSelectionPanelUI.
    public static string TargetScene { get; set; } = null; // Default to null (no specific scene)

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!string.IsNullOrEmpty(TargetScene))
        {
            // Attempt to load by name first.
            // You could also try to parse TargetScene as an int for build index.
            SceneManager.LoadScene(TargetScene);
            Debug.Log($"Loading specified scene: {TargetScene}");
        }
        else
        {

            // If TargetScene is null or empty, do nothing
            return;
            
            /*
            // Default behavior: Load the next scene in the build index.
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            int nextSceneIndex = currentSceneIndex + 1;

            // Check if the next scene index is valid
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextSceneIndex);
                Debug.Log($"Loading next scene in build index: {nextSceneIndex}");
            }
            else
            {
                Debug.LogWarning("No more scenes in build settings to load as 'next scene'. Consider looping back to the main menu or first level.");
                // Optional: Load the first scene as a fallback
                // SceneManager.LoadScene(0);
            }
            */
        }

        // Reset TargetScene after loading so it doesn't affect the next default transition.
        TargetScene = null;
    }

    // It's good practice to reset static variables if the game might be reloaded
    // or if the domain reloads (e.g., exiting play mode in editor).
    #if UNITY_EDITOR
    [UnityEditor.InitializeOnEnterPlayMode]
    static void OnEnterPlaymodeInEditor(UnityEditor.EnterPlayModeOptions options)
    {
        TargetScene = null;
    }
    #endif

    // If you have a central game manager that handles game resets,
    // you might also want to call ResetTargetScene() from there.
    public static void ResetTargetScene()
    {
        TargetScene = null;
    }
}