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
    public GameObject objectiveListingPrefabFallback = default;
    
    [Header("UI Containers")] [SerializeField]
    public GameObject objectiveListingsUIHolder = default;
    private List<GameObject> objectiveListingsUI = new();
    
    [Header("Get Objective Listings From"), Tooltip("Drag and drop the gameobject that holds all your objective listings in here.")]
    [SerializeField] private GameObject objectiveListingsHolder = default;
        
    [Header("Managing Variables (Populates during runtime)")]
    [SerializeField] private List<ObjectiveListing> objectiveListings = new();

    [Header("Objective Tracker Optionals")]
    public bool tapeTogetherObjectives;
    public float tapeSpacing = 5f;
    public bool messyObjectives;
    public float messyObjectiveTilt = 5f;
    
    private bool isClosed = true;
    private Animator animator;
    
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

        // After UI is built, restore any pending objective states (game-over reset or save load)
        StartCoroutine(RestorePendingObjectiveStates());
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
            var prefabToUse = !listingObject.objectiveListingPrefab ? objectiveListingPrefabFallback : listingObject.objectiveListingPrefab;
            
            objectiveListingsUI.Add(
                ObjectiveUIFactory.CreateObjectiveListingUI(listingObject, prefabToUse, objectiveUIPrefab, objectiveListingsUIHolder, ObjectiveListing.ObjectiveToUI));
            
            //create all corresponding individual UI for the objective listing (probably going to move into some sort of object pool)
        }
        SetMessyObjectives(messyObjectives);
    }

    /// <summary>
    /// Waits one frame (so all Start methods finish), then restores any
    /// previously completed objectives and fetch-item progress that was
    /// captured before the scene reload (game-over reset or save load).
    /// </summary>
    private IEnumerator RestorePendingObjectiveStates()
    {
        yield return null; // let every Start() finish first

        var pendingStates = GameManager.Instance?.GetPendingObjectiveStates();
        if (pendingStates == null || pendingStates.Count == 0) yield break;

        // --- Phase 1: Re-give NPC objectives that were previously given ---
        // Build a set of objectives already known in listings
        var knownKeys = new HashSet<string>();
        foreach (var listing in objectiveListings)
        {
            foreach (var obj in listing.objectives)
            {
                if (obj != null)
                    knownKeys.Add(obj.gameObject.name + "|" + obj.description);
            }
        }

        // For each saved objective not in any listing, find the DialogueTrigger that owns it
        foreach (var saved in pendingStates)
        {
            string key = saved.objectiveName + "|" + saved.description;
            if (knownKeys.Contains(key)) continue;

            foreach (var trigger in FindObjectsOfType<DialogueTrigger>())
            {
                var toGive = trigger.ObjectivesToGive;
                if (toGive == null || toGive.Count == 0) continue;

                bool found = toGive.Any(o => o != null
                    && o.gameObject.name == saved.objectiveName
                    && o.description == saved.description);

                if (!found) continue;

                // Mark the NPC as having already given its objectives
                trigger.MarkObjectiveGiven();

                // Restore the NPC's interaction count so it uses the correct
                // dialogue stage (secondary/tertiary instead of base)
                int count = saved.npcInteractionCount;
                trigger.SetInteractionCount(count > 0 ? count : 1);

                // Only replay NPC events if the objective is still in progress.
                // If it's already complete, events are handled by onRestoreEvents
                // on the Objective itself to avoid unwanted re-triggering.
                if (!saved.isComplete)
                    trigger.InvokeEvents();

                // Now add the objectives to the tracker UI
                AddObjective(toGive);

                // Register all the newly-added objectives so we don't re-add them
                foreach (var o in toGive)
                {
                    if (o != null)
                        knownKeys.Add(o.gameObject.name + "|" + o.description);
                }
                break;
            }
        }

        // --- Phase 2: Restore state for every tracked objective ---
        int restoredCount = 0;

        foreach (var listing in objectiveListings)
        {
            for (int i = 0; i < listing.objectives.Count; i++)
            {
                Objective objective = listing.objectives[i];
                if (objective == null) continue;

                // Match by name + description
                ObjectiveSaveState saved = pendingStates.Find(
                    s => s.objectiveName == objective.gameObject.name
                      && s.description   == objective.description);

                if (saved == null) continue;

                // Let the subclass restore its own data (fetch items, tallies, etc.)
                objective.RestoreState(saved);

                if (saved.isComplete)
                {
                    objective.isComplete = true;
                    restoredCount++;

                    // Re-apply world-state side effects (e.g. lights, animations)
                    objective.InvokeRestoreEvents();

                    // Update the UI row directly (no event fire, no double-counting)
                    if (i < listing.objectiveUIList.Count)
                    {
                        listing.objectiveUIList[i]?.SetCompletedVisual();
                    }
                    else if (ObjectiveListing.ObjectiveToUI.TryGetValue(objective, out var ui))
                    {
                        ui?.SetCompletedVisual();
                    }
                }
            }
        }

        // Sync the completed-task counter to the restored value
        GameManager.Instance?.SetNumTasksCompleted(restoredCount);

        GameManager.Instance?.ClearPendingObjectiveStates();
        Debug.Log($"Restored {restoredCount} completed objective(s) from pending state.");
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
        if(objectiveListingsUI.Contains(listing.gameObject)) Destroy(objectiveListingsUI?.ElementAt(objectiveListings.IndexOf(listing)).gameObject);
        if(objectiveListings.Contains(listing)) Destroy(objectiveListings?.ElementAt(objectiveListings.IndexOf(listing)).gameObject);
        
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

    public void AddObjective(List<Objective> objective)
    {
        objectiveListingsUI.Add(
                ObjectiveUIFactory.AddToObjectiveToListingUI(objectiveListings.First(), objective, 
                    objectiveListingPrefabFallback, objectiveUIPrefab, objectiveListingsUI.First(), ObjectiveListing.ObjectiveToUI));
        Debug.LogWarning($"{objectiveListings.First().gameObject.name} for objective {objective}");
            //create all corresponding individual UI for the objective listing (probably going to move into some sort of object pool)
    }
}
