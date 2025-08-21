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
            if (!GameManager.Instance.isGameOver)
                currentTime -= Time.deltaTime;

            UpdateUI();
        }
        else
        {
            currentTime = 0;
            if (GameManager.Instance != null && !GameManager.Instance.isGameOver)
            {
                GameManager.Instance?.GameOver();
                currentTime = totalTime; // Reset timer for next round
            }
        }
    }

    private void UpdateUI()
    {
        if (timerText != null)
        {
            timerText.text = "Deadline: " + currentTime.ToString("F0") + " seconds";
        }

        if (timerSlider != null)
        {
            timerSlider.value = currentTime;
        }
    }

    public void RestartScene()
    {
        currentTime = totalTime;
        Player.Instance.canMoveToggle(true);
        GameManager.Instance?.Reset();
    }

    public void Quit()
    {
        GameManager.Instance?.Quit();
    }
}
