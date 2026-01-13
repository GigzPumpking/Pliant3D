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
    
    [Header("Tutorial Sticky Note")]
    [SerializeField] private Sprite StickyNoteGraphicKeyboard;
    [SerializeField] private Sprite StickyNoteGraphicController;
    
    [Header("Don't Touch!")]
    [SerializeField] private Sprite StickyNoteGraphicFallBack;
    [SerializeField] private ObjectiveListing _objectiveListing;
    [SerializeField] private TutorialStickyNote _tutorialStickyNote;
    private UnityEvent ue;  
    
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
            box.center = Vector3.zero;
            Gizmos.color = _boxColors[idx];
            Gizmos.matrix = coll.transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero,
                new Vector3(box.size.x, box.size.y, box.size.z)
            );
            idx++;
        }
    }
    #endif
    private void OnEnterTutorialBox(bool set, GameObject go)
    {
        if(go != enterBox.gameObject) return;
        _tutorialStickyNote.OnShow();
    }
    

    public void CompleteTutorialSection()
    {
        Debug.LogWarning($"Dropping barrier: {exitColliderBox?.gameObject.name}");
        
        StartCoroutine(_tutorialStickyNote.CompleteTask());
        
        //potentially just disable or destroy the entire tutorial area after completion?
        automaticDialogueTrigger.gameObject.SetActive(false);
        exitColliderBox?.gameObject.SetActive(false);
        exitBox.enabled = false;
        
        
        EnterTutorialBox.OnEnter -= OnEnterTutorialBox;
    }
}
