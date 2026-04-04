using System;
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
    private bool hasStarted = false;
    public bool startAutomatically = true;

    public bool HasStarted => hasStarted;
    public float GetCurrentTime() => currentTime;

    void Start()
    {
        currentTime = 0;
        timerText.text = "";
        timerSlider.gameObject.SetActive(false);

        // Restore pending timer time in Start() before any restore-path InvokeEvents() calls.
        float pendingTime = GameManager.Instance?.GetPendingTimerTime() ?? -1f;
        Debug.Log($"[ObjectiveTimer.Start] pendingTime={pendingTime:F1}, startAutomatically={startAutomatically}");
        if (pendingTime > 0f)
        {
            hasStarted = true;
            currentTime = pendingTime;
            timerSlider.gameObject.SetActive(true);
            if (timerSlider != null)
            {
                timerSlider.maxValue = totalTime;
                timerSlider.value = currentTime;
            }
            SetTimeInMinutesAndSeconds(currentTime);
            UpdateUI();
            GameManager.Instance?.ClearPendingTimerTime();
        }
        else if (startAutomatically)
        {
            StartTimer();
        }
    }

    public void StartTimer()
    {
        // If the timer is already running (restored from pending state), ignore
        // all subsequent calls — there may be multiple from the restore path.
        if (hasStarted)
        {
            Debug.Log($"[ObjectiveTimer.StartTimer] skipped — already running at {currentTime:F1}s");
            return;
        }

        Debug.Log($"[ObjectiveTimer.StartTimer] starting fresh");
        hasStarted = true;
        currentTime = totalTime;
        timerSlider.gameObject.SetActive(true);
        
        if (timerSlider != null)
        {
            timerSlider.maxValue = totalTime;
            timerSlider.value = totalTime;
        }
        SetTimeInMinutesAndSeconds(currentTime);
        UpdateUI();
    }

    void Update()
    {
        if (currentTime > 0 && hasStarted)
        {
            if (!GameManager.Instance.isGameOver)
            {
                currentTime -= Time.deltaTime;
                SetTimeInMinutesAndSeconds(currentTime);
            }

            UpdateUI();
        }
        else
        {
            currentTime = 0;
            if (GameManager.Instance != null && !GameManager.Instance.isGameOver && hasStarted)
            {
                Debug.LogWarning("Game Over from ObjectiveTimer.cs");
                GameManager.Instance?.SetTimerFailed();
                GameManager.Instance?.GameOver();
                currentTime = totalTime; // Reset timer for next round
            }
        }
    }

    private void UpdateUI()
    {
        if (timerText != null)
        {
            timerText.text = "Deadline: " + CurrentTimeInMinutesAndSeconds.Item1.ToString("00") + ":" + CurrentTimeInMinutesAndSeconds.Item2.ToString("00");
        }

        if (timerSlider != null)
        {
            timerSlider.value = currentTime;
        }
    }

    private (float, float) CurrentTimeInMinutesAndSeconds;
    internal Tuple<float, float> ReturnInMinutesAndSeconds(float timeInSeconds)
    {
        return new Tuple<float, float>(
            Mathf.Floor(timeInSeconds / 60), 
            Mathf.Floor(timeInSeconds % 60)
            );
    }
    
    internal void SetTimeInMinutesAndSeconds(float timeInSeconds)
    {
        CurrentTimeInMinutesAndSeconds = (timeInSeconds / 60, timeInSeconds % 60);
    }

    public void RestartScene()
    {
        // Let GameManager.Reset() capture the timer state BEFORE we clear it.
        GameManager.Instance?.Reset();

        currentTime = 0;
        timerText.text = "";
        timerSlider.gameObject.SetActive(false);
        hasStarted = false;
        currentTime = totalTime;
        Player.Instance.canMoveToggle(true);
    }

    public void Quit()
    {
        GameManager.Instance?.Quit();
    }
}
