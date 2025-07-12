using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Tutorializer : MonoBehaviour
{
    [SerializeField] private Collider tutorialBox;
    [SerializeField] private Collider enterBox;
    [SerializeField] private Collider exitBox;
    [SerializeField] private Collider exitColliderBox;
    [SerializeField] private Sprite exitPrompt;
    [SerializeField] private AutomaticDialogueTrigger automaticDialogueTrigger;

    private readonly Dictionary<int, Color> _boxColors = new Dictionary<int, Color>()
    {
        {1, new Color(0,0,255, 0.3f)}, 
        {2, new Color(0,255,0, 0.3f)},  
        {3, new Color(255,0,0, 0.3f)},
        {4, new Color(0,0,0, 0.3f)}
    };

    private void Start()
    {
        SetImageDependency(exitBox, exitPrompt);
    }
    
    private void OnDrawGizmos()
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
    
    private void SetImageDependency(Collider coll, Sprite sprite)
    {
        coll.TryGetComponent<ExitTutorialZoneBox>(out ExitTutorialZoneBox curr);
        curr.imgObject.TryGetComponent<Image>(out Image imgObj); 
        if(imgObj != null) imgObj.sprite = sprite;
    }

    public void CompleteTutorialSection()
    {
        Debug.LogError($"Dropping barrier: {exitColliderBox?.gameObject.name}");
        automaticDialogueTrigger.gameObject.SetActive(false);
        exitColliderBox?.gameObject.SetActive(false);
        exitBox.enabled = false;
    }
}
