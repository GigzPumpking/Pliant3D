using UnityEngine;

public class NextSceneHolder : MonoBehaviour
{
    public string sceneName;

    [Header("Dependency")]
    [Tooltip("Optional. If assigned, this trigger will only work after the referenced AnimTrigger has been triggered.")]
    [SerializeField] private AnimTrigger requiredAnimTrigger;
    [Tooltip("If enabled, load the next scene as soon as the required AnimTrigger fires instead of waiting for the player's collider to enter this trigger.")]
    [SerializeField] private bool triggerOnAnimTrigger = false;

    [Header("Objective Sync")]
    [Tooltip("Optional. Assign the ObjectiveNode for the 'Clock Out' task to ensure it completes before transitioning.")]
    [SerializeField] private ObjectiveNode clockOutNode;

    private bool IsActive => requiredAnimTrigger == null || requiredAnimTrigger.IsTriggered;
    private bool Collided = false;
    private float lastLoadTime = float.MinValue;

    void Update()
    {
        // Don't wait for the player's collider when a required AnimTrigger is assigned -
        // fire the next scene logic as soon as that trigger fires.
        if (triggerOnAnimTrigger && requiredAnimTrigger != null && requiredAnimTrigger.IsTriggered && !Collided)
        {
            LoadNextScene();
            Collided = true;
        }
    }


    public void QuitGame()
    {
        GameManager.Instance?.Quit();
    }
    
    public void LoadNextScene()
    {
        if (Time.unscaledTime - lastLoadTime < 3f) return;
        lastLoadTime = Time.unscaledTime;

        if (clockOutNode != null && !clockOutNode.isComplete)
        {
            clockOutNode.ForceComplete();
        }

        if (UIManager.Instance != null)
        {
            Debug.Log($"Loading scene '{sceneName}' with fade transition.");
            UIManager.Instance?.Resume();
            UIManager.Instance?.LoadSceneWithFade(sceneName);
        }
        else
        {
            Debug.LogWarning("UIManager instance is not available. Loading scene without fade.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"NextSceneHolder: Trigger entered by '{other.name}'. IsActive: {IsActive}, Collided: {Collided}");
        if (!IsActive) return;
        if (Collided) return;

        if (other.CompareTag("Player"))
        {
            LoadNextScene();
            Collided = true;
        }
    }
}
