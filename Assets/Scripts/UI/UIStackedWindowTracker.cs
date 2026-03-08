using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIStackedWindowTracker : MonoBehaviour
{
    public static UIStackedWindowTracker Instance { get; private set; }

    // Simple list where the last element is the top of the stack
    private readonly List<UIPopUpWindow> _stack = new List<UIPopUpWindow>();

    // Windows waiting to have their sibling index set once it's safe
    private readonly List<UIPopUpWindow> _pendingSetSibling = new List<UIPopUpWindow>();

    // Frame-deferred actions (replacements for coroutines)
    private bool _recalibrateNextFrame = false;
    private GameObject _deferredSelectTarget = null;
    private int _deferredSelectAttempts = 0;
    private const int MaxDeferredSelectAttempts = 4; // try a few frames

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Register any already-active popup windows in the scene (bottom -> top)
        InitializeFromScene();
    }

    private void Update()
    {
        // Process pending SetAsLastSibling requests first (try to run them when UI is stable)
        if (_pendingSetSibling.Count > 0)
        {
            // Copy and clear to avoid modification during iteration
            var copy = _pendingSetSibling.ToArray();
            _pendingSetSibling.Clear();
            foreach (var w in copy)
            {
                if (w == null) continue;
                var parent = w.transform.parent;
                // Only set sibling when both the window and its parent are active in hierarchy
                if (w.gameObject.activeInHierarchy && parent != null && parent.gameObject.activeInHierarchy)
                {
                    try { w.gameObject.transform.SetAsLastSibling(); }
                    catch { /* ignore and retry next frame */ }
                }
                else
                {
                    // re-enqueue for next frame
                    if (!_pendingSetSibling.Contains(w)) _pendingSetSibling.Add(w);
                }
            }
        }

        // Process a deferred recalibrate once per frame when requested
        if (_recalibrateNextFrame)
        {
            _recalibrateNextFrame = false;
            RecalibrateCurrentSelected();
        }

        // If there's a deferred select target, attempt to set it. Try for a few frames in case UI isn't ready yet.
        if (_deferredSelectTarget != null && EventSystem.current != null)
        {
            // Try setting selection
            Canvas.ForceUpdateCanvases();
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(_deferredSelectTarget);

            // If successful or we've exhausted attempts, clear the deferred target
            if (EventSystem.current.currentSelectedGameObject != null || ++_deferredSelectAttempts >= MaxDeferredSelectAttempts)
            {
                _deferredSelectTarget = null;
                _deferredSelectAttempts = 0;
            }
        }
    }

    // Register a window: if it's already in the stack, move it to the top; otherwise push it.
    public void RegisterWindow(UIPopUpWindow window)
    {
        if (window == null) return;

        // Cache currently selected or a sensible fallback for the previous top window
        if (_stack.Count > 0)
        {
            var prevTop = _stack[_stack.Count - 1];
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == null || !selected.activeInHierarchy)
            {
                if (prevTop.defaultButton != null && prevTop.defaultButton.activeInHierarchy)
                    selected = prevTop.defaultButton;
                else
                    selected = FindFirstInteractableSelectable(prevTop);
            }
            prevTop.SetLastActivatedButton(selected);
        }

        EventSystem.current?.SetSelectedGameObject(window.defaultButton != null
            ? window.defaultButton
            : window.GetLastActivatedButton());

        // If already in stack, remove it so we can move it to top
        _stack.Remove(window);
        _stack.Add(window);

        // Queue SetAsLastSibling so we don't try to change sibling during parent activation/deactivation
        if (window != null && !_pendingSetSibling.Contains(window)) _pendingSetSibling.Add(window);

        // Defer recalibrate by one frame (replacement for DeferredRecalibrate coroutine)
        _recalibrateNextFrame = true;
    }

    // Unregister a window: remove it from the stack and recalibrate selection to the new top
    public void UnregisterWindow(UIPopUpWindow window)
    {
        if (window == null) return;

        bool removed = _stack.Remove(window);
        if (!removed) return;

        // If there's a new top, ensure it's active and schedule it to be brought to front
        if (_stack.Count > 0)
        {
            var newTop = _stack[_stack.Count - 1];
            if (newTop != null)
            {
                newTop.gameObject.SetActive(true);
                if (!_pendingSetSibling.Contains(newTop)) _pendingSetSibling.Add(newTop);
            }
        }

        // Defer recalibrate by one frame
        _recalibrateNextFrame = true;
    }

    // Recalibrate selection to the top window's best choice (lastActivated -> default -> first interactable)
    private void RecalibrateCurrentSelected()
    {
        if (EventSystem.current == null) return;
        if (_stack.Count == 0)
        {
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        var top = _stack[_stack.Count - 1];
        if (top == null) { EventSystem.current.SetSelectedGameObject(null); return; }

        GameObject desired = top.GetLastActivatedButton();
        if (desired == null || !desired.activeInHierarchy)
        {
            if (top.defaultButton != null && top.defaultButton.activeInHierarchy)
                desired = top.defaultButton;
            else
                desired = FindFirstInteractableSelectable(top);
        }

        if (desired != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(desired);

            // If Unity didn't accept the selection immediately, schedule a few frame attempts without coroutines
            if (EventSystem.current.currentSelectedGameObject == null)
            {
                _deferredSelectAttempts = 0;
                _deferredSelectTarget = desired;
            }
        }
        else if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // Helper: find the first active & interactable Selectable under the window
    private GameObject FindFirstInteractableSelectable(UIPopUpWindow window)
    {
        if (window == null) return null;
        var selectables = window.GetComponentsInChildren<Selectable>(true);
        foreach (var s in selectables)
        {
            if (s != null && s.IsActive() && s.IsInteractable()) return s.gameObject;
        }
        return null;
    }


    private void InitializeFromScene()
    {
        // Find active UIPopUpWindow instances and register them in visual order (bottom -> top)
        var all = FindObjectsOfType<UIPopUpWindow>(true);
        if (all == null || all.Length == 0) return;

        // Build list ordered by sibling index
        var list = new List<UIPopUpWindow>();
        foreach (var w in all)
            if (w != null && w.gameObject.activeInHierarchy) list.Add(w);

        list.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        foreach (var w in list)
            RegisterWindow(w);
    }
}
