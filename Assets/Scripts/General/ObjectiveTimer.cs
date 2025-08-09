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

    [SerializeField] private GameObject gameOverPanel;

    private bool isGameOver = false;

    void Start()
    {
        currentTime = totalTime;

        if (timerSlider != null)
        {
            timerSlider.maxValue = totalTime;
            timerSlider.value = totalTime;
        }

        gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            UpdateUI();
        }
        else if (!isGameOver)
        {
            currentTime = 0;

            gameOverPanel.SetActive(true);

            Player.Instance.canMoveToggle(false);

            isGameOver = true;
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
        // Reset the time scale to normal

        currentTime = totalTime;

        Player.Instance.canMoveToggle(true);

        GameManager.Instance?.Reset();

        isGameOver = false;
    }

    public void Quit()
    {
        GameManager.Instance?.Quit();
    }
}
