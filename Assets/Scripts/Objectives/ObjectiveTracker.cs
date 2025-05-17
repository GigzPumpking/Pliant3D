using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using System;

public class ObjectiveTracker : MonoBehaviour {
    [Header("Prefabs"), Tooltip("The format of how Objective UI's will be presented.")]
    public GameObject objectiveUIPrefab = default;
    public GameObject objectiveListingPrefab = default;
    
    [Header("Hierarchy"), Tooltip("References to dependencies within the hierarchy")] 
    public GameObject objectiveListingsUIHolder = default;
    
    [Header("Managing Variables")]
    [SerializeField] private List<ObjectiveListing> objectiveListings = new();
    [SerializeField] private GameObject objectiveListingsHolder = default;

    [Header("Managing UI")] [SerializeField]
    private List<GameObject> objectiveListingsUI = new();
    
    private bool isClosed = true;
    private Animator animator;

    

    private void OnEnable() {
        
    }
    
    void Start()
    {
        animator = GetComponent<Animator>();
        
        //get each objective listing within the gameobject 'objectiveListingsHolder'
        foreach (ObjectiveListing listingObject in objectiveListingsHolder.GetComponentsInChildren<ObjectiveListing>()) {
            //add it to the tracker
            objectiveListings.Add(listingObject);
            //add an instance of the Objective Listing UI to the tracker
            objectiveListingsUI.Add(
                ObjectiveUIFactory.CreateObjectiveListingUI(listingObject, objectiveListingPrefab, objectiveUIPrefab, objectiveListingsUIHolder));
            
            //create all corresponding individual UI for the objective listing (probably going to move into some sort of object pool)
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            switch (isClosed)
            {
                case true:
                    OpenTracker();
                    break;
                case false:
                    CloseTracker();
                    break;
            }
        }
    }

    void OpenTracker()
    {
        animator.SetBool("TrackerOpen", true);
        isClosed = false;
    }

    void CloseTracker()
    {
        animator.SetBool("TrackerOpen", false);
        isClosed = true;
    }

    //if the entire listing is complete, then 
    private void UICompleteObjective(ObjectiveListing objectiveListings) {

    }
}
