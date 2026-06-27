using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Tutorializer : MonoBehaviour
{
    [Header("Tutorial Area GameObjects")]
    [SerializeField] private Collider tutorialBox;
    [SerializeField] private Collider enterBox;
    [SerializeField] private Collider exitBox;
    [SerializeField] private Collider exitColliderBox;
    [SerializeField] private AutomaticDialogueTrigger automaticDialogueTrigger;
    [Tooltip("All GameObjects here will be disabled when the tutorial is completed or restored as complete on load/reset.")]
    [SerializeField] private List<GameObject> tutorialAreaObjects;
    
    [Header("Tutorial Sticky Note")]
    [SerializeField] private Sprite StickyNoteGraphicKeyboard;
    [SerializeField] private Sprite StickyNoteGraphicController;
    [Tooltip("The root GameObject of the sticky note UI. Disabled immediately on load/reset restore; disabled after the completion animation on first completion.")]
    [SerializeField] private GameObject stickyNoteObject;
    
    [Header("Don't Touch!")]
    [SerializeField] private Sprite StickyNoteGraphicFallBack;
    [SerializeField] private ObjectiveListing _objectiveListing;
    [SerializeField] private TutorialStickyNote _tutorialStickyNote;

	[Header("SoundFX")]
	[SerializeField] private AudioData tutorialCompleteSFX;
    private UnityEvent ue;

    private bool _isComplete = false;
    
    private readonly Dictionary<int, Color> _boxColors = new Dictionary<int, Color>()
    {
        {1, new Color(0,0,255, 0.3f)},
        {2, new Color(0,255,0, 0.3f)},
        {3, new Color(255,0,0, 0.3f)},
        {4, new Color(0,0,0, 0.3f)}
    };
    
    private void Start()
    {
        if (!_objectiveListing) _objectiveListing = GetComponentInChildren<ObjectiveListing>();
        
        ue = new UnityEvent();
        ue.AddListener(CompleteTutorialSection);
        _objectiveListing.AddCompletionEvents(ue);

        StartCoroutine(CheckRestoredState());
    }

    private void OnEnable()
    {
        EnterTutorialBox.OnEnter += OnEnterTutorialBox;
    }
    
    private void OnDisable()
    {
        EnterTutorialBox.OnEnter -= OnEnterTutorialBox;
        ue.RemoveAllListeners();
    }

    private void OnDrawGizmosSelected()
    { 
#if UNITY_EDITOR
        if (!tutorialBox) goto enterBoxSetup;
        switch (tutorialBox)
        {
            case BoxCollider box:
                DrawBox(tutorialBox, enterBox, exitBox, exitColliderBox);
                break;
            case SphereCollider sphere:Debug.LogError("Sphere collider");
                break;
            case MeshCollider mesh:Debug.LogError("Mesh collider");
                break;
            default:
                break;
        }
        enterBoxSetup: ;
        if (!enterBox) goto exitBoxSetup;
        
        exitBoxSetup: ;
        
        if (!exitBox) goto setupPhase;
        
        setupPhase: ;

#endif
    }

    private void OnValidate()
    {
        if (!_tutorialStickyNote) return;
        
        _tutorialStickyNote.StickyNoteGraphicController =
            StickyNoteGraphicController ? StickyNoteGraphicController : StickyNoteGraphicFallBack;

        _tutorialStickyNote.StickyNoteGraphicKeyboard =
            StickyNoteGraphicKeyboard ? StickyNoteGraphicKeyboard : StickyNoteGraphicFallBack;
    }

    #if UNITY_EDITOR
    private void DrawBox(params Collider[] colliders)
    {
        int idx = 1;
        foreach (Collider coll in colliders)
        {
            BoxCollider box = coll?.GetComponent<Collider>() as BoxCollider;
            //box.center = Vector3.zero;
            Gizmos.color = _boxColors[idx];
            Gizmos.matrix = coll.transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center,
                new Vector3(box.size.x, box.size.y, box.size.z)
            );
            idx++;
        }
    }
    #endif
    private void OnEnterTutorialBox(bool set, GameObject go)
    {
        if (go != enterBox.gameObject) return;
        if (_isComplete) return;
        _tutorialStickyNote.OnShow();
    }

    public void CompleteTutorialSection()
    {
        if (_isComplete) return;
        StartCoroutine(CompleteTutorialSectionCoroutine());
    }

    private IEnumerator CompleteTutorialSectionCoroutine()
    {
        // Guard against re-entry during the animation window.
        _isComplete = true;
        EnterTutorialBox.OnEnter -= OnEnterTutorialBox;

        Debug.LogWarning($"Dropping barrier: {exitColliderBox?.gameObject.name}");

        // Play stamp animation and sound, then hide the sticky note (1.5 s).
        yield return _tutorialStickyNote.CompleteTask(tutorialCompleteSFX);

        // Now that the animation has finished, apply all world-state consequences.
        ApplyCompletedState();
    }

    private void ApplyCompletedState()
    {
        _isComplete = true;
        automaticDialogueTrigger.gameObject.SetActive(false);
        exitColliderBox?.gameObject.SetActive(false);
        exitBox.enabled = false;
        if (enterBox) enterBox.enabled = false;
        EnterTutorialBox.OnEnter -= OnEnterTutorialBox;
        stickyNoteObject?.SetActive(false);

        if (tutorialAreaObjects != null)
        {
            foreach (var obj in tutorialAreaObjects)
                obj?.SetActive(false);
        }
    }

    private IEnumerator CheckRestoredState()
    {
        // Cache pending states NOW — ObjectiveTracker's own coroutine yields one frame then clears them.
        // Tutorial ObjectiveListings are outside the "Objective Listings" holder so ObjectiveTracker
        // never restores them; we must detect completion here against the raw save data.
        var pendingStates = GameManager.Instance?.GetPendingObjectiveStates();

        // Yield one frame so ObjectiveListing.Start() / EnsureNonEmpty() has time to populate
        // the objectives list before we try to read it.
        yield return null;

        if (_isComplete || _objectiveListing == null || pendingStates == null || pendingStates.Count == 0)
            yield break;

        var objectives = _objectiveListing.objectives;

        // Fallback: if the serialized list is still empty, search children (including inactive).
        if (objectives == null || objectives.Count == 0)
        {
            var found = _objectiveListing.GetComponentsInChildren<Objective>(true);
            if (found == null || found.Length == 0) yield break;
            objectives = new List<Objective>(found);
        }

        bool allComplete = objectives.TrueForAll(obj =>
        {
            if (obj == null) return false;
            var saved = pendingStates.Find(s =>
                s.objectiveName == obj.gameObject.name &&
                s.description == obj.description);
            return saved != null && saved.isComplete;
        });

        if (allComplete)
            ApplyCompletedState();
    }
}
