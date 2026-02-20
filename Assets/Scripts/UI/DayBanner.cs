using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Displays a day-of-the-week banner when certain levels first load.
/// Attach to a UI Canvas (Screen Space – Overlay) under the UIManager.
/// Populate <see cref="dayEntries"/> with one entry per level, pairing
/// the scene name with the UI panel to display.
/// A CanvasGroup is added at runtime to each panel if one is not already present.
/// </summary>
public class DayBanner : MonoBehaviour
{
    [Serializable]
    public struct DayEntry
    {
        [Tooltip("The scene name that triggers this banner (e.g. 1-1).")]
        public string levelName;

        [Tooltip("The UI object to display for this level.")]
        public GameObject panel;
    }

    [Tooltip("Each entry pairs a scene name with the banner UI to show for that level.")]
    [SerializeField] private List<DayEntry> dayEntries = new List<DayEntry>();

    [Tooltip("How long (seconds) the banner stays visible at the center before fading.")]
    [SerializeField] private float displayDuration = 3f;

    [Header("Drop-In Animation")]
    [Tooltip("How long (seconds) the drop-in tween takes.")]
    [SerializeField] private float dropDuration = 0.5f;

    [Tooltip("How far above the centre (in canvas units) the panel starts.")]
    [SerializeField] private float dropOffset = 600f;

    [Tooltip("Ease curve for the drop-in (0→1). Defaults to a fast-start, soft-land ease-out.")]
    [SerializeField] private AnimationCurve dropCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),   // start: zero value, steep tangent (fast acceleration)
        new Keyframe(1f, 1f, 0f, 0f)    // end: full value, flat tangent (gentle deceleration)
    );

    [Header("Fade-Out Animation")]
    [Tooltip("How long (seconds) the fade-out takes.")]
    [SerializeField] private float fadeDuration = 0.5f;

    private Coroutine activeRoutine;

    private void OnEnable()
    {
        EventDispatcher.AddListener<NewSceneLoaded>(OnNewSceneLoaded);
    }

    private void OnDisable()
    {
        EventDispatcher.RemoveListener<NewSceneLoaded>(OnNewSceneLoaded);
    }

    private void Start()
    {
        // Ensure every panel has a CanvasGroup for fading and starts hidden.
        foreach (DayEntry entry in dayEntries)
        {
            if (entry.panel == null) continue;
            EnsureCanvasGroup(entry.panel);
        }
        HideAll();
    }

    private void OnNewSceneLoaded(NewSceneLoaded e)
    {
        for (int i = 0; i < dayEntries.Count; i++)
        {
            if (dayEntries[i].levelName == e.sceneName)
            {
                Show(i);
                return;
            }
        }
    }

    private void Show(int index)
    {
        // Stop any banner that is already being displayed.
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            HideAll();
        }

        GameObject panel = dayEntries[index].panel;
        if (panel == null) return;

        CanvasGroup cg = EnsureCanvasGroup(panel);
        RectTransform rt = panel.GetComponent<RectTransform>();

        // Anchor to centre; the panel will land halfway between the top and the centre.
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            // Start above the screen.
            rt.anchoredPosition = new Vector2(0f, dropOffset);
        }

        // Reset alpha to fully visible.
        cg.alpha = 1f;

        panel.SetActive(true);
        activeRoutine = StartCoroutine(AnimateBanner(panel, rt, cg));
    }

    private IEnumerator AnimateBanner(GameObject panel, RectTransform rt, CanvasGroup cg)
    {
        // --- Drop-in ---
        Vector2 startPos = new Vector2(0f, dropOffset);
        Vector2 endPos = new Vector2(0f, dropOffset * 0.5f); // stop midway between top and centre

        float elapsed = 0f;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dropDuration);
            float curved = dropCurve.Evaluate(t);
            if (rt != null) rt.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, curved);
            yield return null;
        }
        if (rt != null) rt.anchoredPosition = endPos;

        // --- Hold ---
        yield return new WaitForSeconds(displayDuration);

        // --- Fade-out ---
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            cg.alpha = 1f - t;
            yield return null;
        }
        cg.alpha = 0f;

        if (panel != null) panel.SetActive(false);
        activeRoutine = null;
    }

    /// <summary>
    /// Guarantees a CanvasGroup exists on the panel so we can fade its alpha.
    /// </summary>
    private CanvasGroup EnsureCanvasGroup(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        return cg;
    }

    private void HideAll()
    {
        foreach (DayEntry entry in dayEntries)
        {
            if (entry.panel == null) continue;
            CanvasGroup cg = entry.panel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;
            entry.panel.SetActive(false);
        }
    }
}
