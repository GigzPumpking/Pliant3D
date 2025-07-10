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

    public bool messyObjectives;
    public float messyObjectiveTilt = 5f;
    
    private void OnEnable() {
        ObjectiveListing.OnObjectiveListingComplete += UICompleteObjective;
        NextSceneTrigger.NextSceneTriggered += ClearAndRefetchObjectives;
    }
    
    private void OnDisable() {
        ObjectiveListing.OnObjectiveListingComplete -= UICompleteObjective;
        NextSceneTrigger.NextSceneTriggered -= ClearAndRefetchObjectives;
    }
    
    void Start()
    {
        animator = GetComponent<Animator>();

        GetObjectiveDependencies();
    }
    
    private void OnValidate()
    {
        SetMessyObjectives(messyObjectives);
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
        if (!objectiveListingsHolder) objectiveListingsHolder = GameObject.Find("Objective Listings");
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
        SetMessyObjectives(messyObjectives);
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
    
    private void UICompleteObjective(ObjectiveListing listing) {
        //destory them
        Destroy(objectiveListingsUI.ElementAt(objectiveListings.IndexOf(listing)).gameObject);
        Destroy(objectiveListings.ElementAt(objectiveListings.IndexOf(listing)).gameObject);
        
        Debug.Log("Destroying UI listing");
    }

    private void ClearAndRefetchObjectives() {
        foreach (ObjectiveListing listing in objectiveListings) {
            UICompleteObjective(listing);
        }
        
        objectiveListings.Clear();
        objectiveListingsUI.Clear();
        GetObjectiveDependencies();
    }

    public void SetMessyObjectives(bool set)
    {
        //rotate to look messy
        if (set)
        {
            int idx = 1;
            foreach (GameObject listing in objectiveListingsUI)
            {
                int flip = (idx % 2) == 0 ? -1 : 1;
                listing.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, messyObjectiveTilt * flip);
                idx++;
            }
        }
        //straight objectives to look neat
        else
        {
            foreach (GameObject listing in objectiveListingsUI)
            {
                listing.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }
    }
}
