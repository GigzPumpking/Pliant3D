using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIStackedWindowTracker : MonoBehaviour
{
    public static UIStackedWindowTracker Instance { get; private set; }

    // Simple list where the last element is the top of the stack
    private readonly List<UIPopUpWindow> _stack = new List<UIPopUpWindow>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Register any already-active popup windows in the scene (bottom -> top)
        InitializeFromScene();
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

        // If already in stack, remove it so we can move it to top
        _stack.Remove(window);
        _stack.Add(window);

        // Ensure the window is visually on top
        window.gameObject.transform.SetAsLastSibling();

        // Make sure selection is valid for the new top
        StartCoroutine(DeferredRecalibrate());
    }

    // Unregister a window: remove it from the stack and recalibrate selection to the new top
    public void UnregisterWindow(UIPopUpWindow window)
    {
        if (window == null) return;

        bool removed = _stack.Remove(window);
        if (!removed) return;

        // If there's a new top, ensure it's active and on top so selection can be applied
        if (_stack.Count > 0)
        {
            var newTop = _stack[_stack.Count - 1];
            if (newTop != null)
            {
                newTop.gameObject.SetActive(true);
                newTop.gameObject.transform.SetAsLastSibling();
            }
        }

        StartCoroutine(DeferredRecalibrate());
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

        if (desired != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(desired);
            if (EventSystem.current.currentSelectedGameObject == null)
                StartCoroutine(DeferredSetSelected(desired));
        }
        else
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

    private IEnumerator DeferredSetSelected(GameObject go)
    {
        yield return null; // wait one frame
        if (EventSystem.current == null) yield break;
        Canvas.ForceUpdateCanvases();
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(go);
    }

    private IEnumerator DeferredRecalibrate()
    {
        yield return null; // wait one frame for UI teardown/setup
        RecalibrateCurrentSelected();
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

    // Optional: expose a read-only snapshot for debugging/tests
    public IList<UIPopUpWindow> GetStackSnapshot() => _stack.AsReadOnly();
}
