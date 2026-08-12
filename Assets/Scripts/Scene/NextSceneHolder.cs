using UnityEngine;
using UnityEngine.UI;

public class NextSceneHolder : MonoBehaviour
{
    public string sceneName;

    [Header("Dependency")]
    [Tooltip("Optional. If assigned, this trigger will only work after the referenced AnimTrigger has been triggered.")]
    [SerializeField] private AnimTrigger requiredAnimTrigger;

    [Tooltip("Optional. If assigned, this button will be disabled once CallLoadNextScene fires.")]
    [SerializeField] private Button nextSceneButton;

    private bool IsActive => requiredAnimTrigger == null || requiredAnimTrigger.IsTriggered;
    private bool Collided = false;
    private bool hasBeenCalled = false;


    public void QuitGame()
    {
        GameManager.Instance?.Quit();
    }

    // Shared entry point for UI buttons and NewSceneChecker — only the first call goes through.
    public void CallLoadNextScene()
    {
        if (hasBeenCalled) return;
        hasBeenCalled = true;
        if (nextSceneButton != null) nextSceneButton.interactable = false;
        LoadNextScene();
    }

    public void LoadNextScene()
    {
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
