using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ObjectiveTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("The total time for the countdown in seconds.")]
    public float totalTime = 60.0f;

    [Header("UI Elements (Optional)")]
    [Tooltip("Assign a UI Text element here to display the remaining time.")]
    public TextMeshProUGUI timerText;

    [Tooltip("Assign a UI Slider element here to visually represent the countdown.")]
    public Slider timerSlider;

    private float currentTime;

    void Start()
    {
        currentTime = totalTime;

        if (timerSlider != null)
        {
            timerSlider.maxValue = totalTime;
            timerSlider.value = totalTime;
        }
    }

    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            UpdateUI();
        }
        else
        {
            currentTime = 0;

            RestartScene();
        }
    }

    private void UpdateUI()
    {
        if (timerText != null)
        {
            timerText.text = "Time Left: " + currentTime.ToString("F0");
        }

        if (timerSlider != null)
        {
            timerSlider.value = currentTime;
        }
    }

    public void RestartScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();

        SceneManager.LoadScene(currentScene.buildIndex);
    }
}
