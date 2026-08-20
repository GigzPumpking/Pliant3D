using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Text element that displays the currently selected resolution.")]
    [SerializeField] private TMP_Text resolutionValueText;

    [Tooltip("Button that selects the previous (lower) resolution in the list.")]
    [SerializeField] private Button resolutionLeftButton;

    [Tooltip("Button that selects the next (higher) resolution in the list.")]
    [SerializeField] private Button resolutionRightButton;

    [Tooltip("Button that applies the current resolution/fullscreen settings.")]
    [SerializeField] private Button applyButton;

    [Header("Canvas Settings")]
    [Tooltip("Canvas Scaler component that will update with the new resolution.")]
    [SerializeField] private CanvasScaler canvasScaler;

    [Header("Display Mode")]
    [Tooltip("Toggle for fullscreen/windowed mode.")]
    [SerializeField] private Toggle fullscreenToggle;
    // Tracks whether the game is in fullscreen mode.
    private bool fullscreenMode = false;
    public bool IsFullscreen => fullscreenMode;

    [Header("Initial Settings")]
    [Tooltip("Index of the initial resolution from the available resolutions list (used for windowed mode).")]
    public int initialResolutionIndex = 0;

    // Predefined fallback list of resolutions.
    [SerializeField]
    private List<Resolution> customResolutions = new List<Resolution>()
    {
        new Resolution { width = 1920, height = 1080 },
        new Resolution { width = 1440, height = 900 },
        // Add more custom resolutions if needed.
    };

    // This list will store either Screen.resolutions or customResolutions.
    private List<Resolution> availableResolutionsList = new List<Resolution>();

    // Index of the currently selected (but not yet necessarily applied) resolution.
    private int currentResolutionIndex = 0;

    private void Awake()
    {
        // Automatically find the CanvasScaler in children if not assigned.
        if (canvasScaler == null)
        {
            canvasScaler = GetComponentInChildren<CanvasScaler>();
        }

        // If a fullscreen toggle is assigned, add a listener.
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggle);
        }

        // If an apply button is assigned, add a listener.
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplySettings);
        }

    }

    private void Start()
    {
        // Build the list of available resolutions.
        PopulateResolutionList();

        // Try to find the player's current screen resolution in our list, regardless of fullscreen state.
        bool found = false;
        for (int i = 0; i < availableResolutionsList.Count; i++)
        {
            Resolution res = availableResolutionsList[i];
            if (res.width == Screen.width && res.height == Screen.height)
            {
                initialResolutionIndex = i;
                found = true;
                break;
            }
        }
        if (!found || initialResolutionIndex < 0 || initialResolutionIndex >= availableResolutionsList.Count)
        {
            initialResolutionIndex = availableResolutionsList.Count - 1;
        }

        // Set the initial selected resolution to match the current window resolution.
        currentResolutionIndex = initialResolutionIndex;
        UpdateResolutionDisplay();

        // Set initial fullscreen mode based on Screen.fullScreen.
        fullscreenMode = Screen.fullScreen;
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = fullscreenMode;
        }
        
        // Apply the initial resolution.
        ApplySettings();
    }

    void Update()
    {
        // Update the fullscreen toggle if the actual fullscreen state changes externally.
        if (fullscreenToggle != null && fullscreenToggle.isOn != Screen.fullScreen)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenMode = Screen.fullScreen;
        }
    }

    void PopulateResolutionList()
    {
        availableResolutionsList.Clear();

        // Try to use Screen.resolutions.
        Resolution[] screenResolutions = Screen.resolutions;
        if (screenResolutions != null && screenResolutions.Length > 0)
        {
            // Screen.resolutions reports one entry per supported refresh rate, so the
            // same width/height can appear several times. Keep only the first (highest
            // refresh rate) entry per width/height to avoid duplicate dropdown options.
            HashSet<(int width, int height)> seenResolutions = new HashSet<(int, int)>();
            foreach (Resolution res in screenResolutions)
            {
                if (seenResolutions.Add((res.width, res.height)))
                {
                    availableResolutionsList.Add(res);
                }
            }
        }
        else
        {
            // Fallback to the predefined custom resolutions.
            availableResolutionsList.AddRange(customResolutions);
        }
    }

    /// Selects the previous (lower) resolution in the list, if not already at the lowest.
    public void SelectPreviousResolution()
    {
        if (currentResolutionIndex > 0)
        {
            currentResolutionIndex--;
            UpdateResolutionDisplay();
        }
    }

    /// Selects the next (higher) resolution in the list, if not already at the highest.
    public void SelectNextResolution()
    {
        if (currentResolutionIndex < availableResolutionsList.Count - 1)
        {
            currentResolutionIndex++;
            UpdateResolutionDisplay();
        }
    }

    /// Updates the resolution text display and the interactable state of the arrow buttons.
    void UpdateResolutionDisplay()
    {
        if (currentResolutionIndex < 0 || currentResolutionIndex >= availableResolutionsList.Count)
        {
            return;
        }

        Resolution res = availableResolutionsList[currentResolutionIndex];
        if (resolutionValueText != null)
        {
            resolutionValueText.text = res.width + " x " + res.height;
        }

        if (resolutionLeftButton != null)
        {
            resolutionLeftButton.interactable = currentResolutionIndex > 0;
        }
        if (resolutionRightButton != null)
        {
            resolutionRightButton.interactable = currentResolutionIndex < availableResolutionsList.Count - 1;
        }
    }

    public void ApplySettings()
    {
        if (currentResolutionIndex < 0 || currentResolutionIndex >= availableResolutionsList.Count)
        {
            Debug.LogWarning("Invalid resolution index.");
            return;
        }

        Resolution selectedResolution = availableResolutionsList[currentResolutionIndex];
        // Set the resolution with the selected mode (fullscreenMode).
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, fullscreenMode);
        // Update the CanvasScaler's reference resolution to match the aspect ratio of the selected resolution.
        UpdateCanvasScalerForResolution(selectedResolution);
    }

    /// Updates the CanvasScaler's reference resolution so that the aspect ratio matches the selected resolution.
    /// The idea is to keep the "size" similar to 1920 x 1080 but adjust the height to match the new aspect ratio.
    /// For example, if the selected resolution is 1440 x 900 (16:10), then set the reference resolution to 1920 x 1200.

    void UpdateCanvasScalerForResolution(Resolution res)
    {
        if (canvasScaler != null)
        {
            float refWidth = 1920f;
            // Calculate new reference height based on the selected resolution's aspect ratio.
            // newReferenceHeight = referenceWidth * (selectedHeight / selectedWidth)
            float refHeight = refWidth * ((float)res.height / res.width);
            canvasScaler.referenceResolution = new Vector2(refWidth, refHeight);
        }
    }

    /// Called when the fullscreen toggle changes.
    /// This method sets fullscreenMode based on the toggle value and applies it.
    /// It also checks the current Screen.fullScreen value and updates if necessary.
    public void OnFullscreenToggle(bool value)
    {
        if (Screen.fullScreen != value)
        {
            Screen.fullScreen = value;
        }
        fullscreenMode = Screen.fullScreen;
        // Note: The user must still click "Apply" to update the resolution and CanvasScaler.
    }
}
