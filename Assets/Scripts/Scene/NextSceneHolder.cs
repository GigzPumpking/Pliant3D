using UnityEngine;

public class NextSceneHolder : MonoBehaviour
{
    public string sceneName;

    [Header("Dependency")]
    [Tooltip("Optional. If assigned, this trigger will only work after the referenced AnimTrigger has been triggered.")]
    [SerializeField] private AnimTrigger requiredAnimTrigger;

    [Header("Objective Sync")]
    [Tooltip("Optional. Assign the ObjectiveNode for the 'Clock Out' task to ensure it completes before transitioning.")]
    [SerializeField] private ObjectiveNode clockOutNode;

    private bool IsActive => requiredAnimTrigger == null || requiredAnimTrigger.IsTriggered;
    private bool Collided = false;

    public void LoadNextScene()
    {
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
