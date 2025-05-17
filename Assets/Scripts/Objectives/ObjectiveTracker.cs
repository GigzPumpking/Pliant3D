using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using System;

public class ObjectiveTracker : MonoBehaviour {
    [Header("UI Prefabs"), Tooltip("The format of how Objective UI's will be presented.")]
    public GameObject objectiveUIPrefab = default;
    public GameObject objectiveListingPrefab = default;
    
    [Header("UI Containers")] [SerializeField]
    public GameObject objectiveListingsUIHolder = default;
    private List<GameObject> objectiveListingsUI = new();
    
    [Header("Get Objective Listings From"), Tooltip("Drag and drop the gameobject that holds all your objective listings in here.")]
    [SerializeField] private GameObject objectiveListingsHolder = default;
        
    [Header("Managing Variables (Populates during runtime)")]
    [SerializeField] private List<ObjectiveListing> objectiveListings = new();
    
    private bool isClosed = true;
    private Animator animator;

    

    private void OnEnable() {
        
    }
    
    void Start()
    {
        animator = GetComponent<Animator>();

        GetObjectiveDependencies();
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

    void GetObjectiveDependencies() {
        if (!objectiveListingsHolder) GameObject.Find("Objective Listings");
        //get each objective listing within the gameobject 'objectiveListingsHolder'
        foreach (ObjectiveListing listingObject in objectiveListingsHolder.GetComponentsInChildren<ObjectiveListing>()) {
            if (!objectiveListingsHolder) return;
            //add it to the tracker
            objectiveListings.Add(listingObject);
            //add an instance of the Objective Listing UI to the tracker
            objectiveListingsUI.Add(
                ObjectiveUIFactory.CreateObjectiveListingUI(listingObject, objectiveListingPrefab, objectiveUIPrefab, objectiveListingsUIHolder));
            
            //create all corresponding individual UI for the objective listing (probably going to move into some sort of object pool)
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
