using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class SettingsMenu : Menu
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;

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

    private void Awake()
    {
        // Ensure the resolution dropdown is assigned.
        if (resolutionDropdown == null)
        {
            resolutionDropdown = GetComponentInChildren<TMP_Dropdown>();
        }

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
        // Populate the resolution dropdown.
        PopulateDropdown();

        // If in fullscreen, try to find the current fullscreen resolution in our list.
        if (Screen.fullScreen)
        {
            bool found = false;
            for (int i = 0; i < availableResolutionsList.Count; i++)
            {
                Resolution res = availableResolutionsList[i];
                // Compare with Screen.currentResolution (or Screen.width/Screen.height)
                if (res.width == Screen.currentResolution.width && res.height == Screen.currentResolution.height)
                {
                    initialResolutionIndex = i;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                initialResolutionIndex = availableResolutionsList.Count - 1;
            }
        }
        // Otherwise, initialResolutionIndex remains as set in the Inspector for windowed mode.
        if (initialResolutionIndex < 0 || initialResolutionIndex >= availableResolutionsList.Count)
        {
            initialResolutionIndex = availableResolutionsList.Count - 1;
        }

        // Set the dropdown's initial value.
        resolutionDropdown.value = initialResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // Set initial fullscreen mode based on Screen.fullScreen.
        fullscreenMode = Screen.fullScreen;
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = fullscreenMode;
        }
        
        // Apply the initial resolution.
        ApplySettings();
    }

    protected override void Update()
    {
        base.Update();
        // Update the fullscreen toggle if the actual fullscreen state changes externally.
        if (fullscreenToggle != null && fullscreenToggle.isOn != Screen.fullScreen)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenMode = Screen.fullScreen;
        }
    }

    void PopulateDropdown()
    {
        List<string> options = new List<string>();

        // Clear any existing options.
        resolutionDropdown.ClearOptions();
        availableResolutionsList.Clear();

        // Try to use Screen.resolutions.
        Resolution[] screenResolutions = Screen.resolutions;
        if (screenResolutions != null && screenResolutions.Length > 0)
        {
            availableResolutionsList.AddRange(screenResolutions);
        }
        else
        {
            // Fallback to the predefined custom resolutions.
            availableResolutionsList.AddRange(customResolutions);
        }

        // Add each resolution to the dropdown options.
        foreach (Resolution res in availableResolutionsList)
        {
            options.Add(res.width + " x " + res.height);
        }

        resolutionDropdown.AddOptions(options);
    }

    public void ApplySettings()
    {
        int index = resolutionDropdown.value;
        if (index < 0 || index >= availableResolutionsList.Count)
        {
            Debug.LogWarning("Invalid resolution index.");
            return;
        }
        
        Resolution selectedResolution = availableResolutionsList[index];
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
