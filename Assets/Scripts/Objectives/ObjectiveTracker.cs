using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectiveTracker : MonoBehaviour
{
    [Header("UI Prefabs"), Tooltip("The format of how Objective UI's will be presented.")]
    public GameObject objectiveUIPrefab = default;

    [Tooltip("This is the visual Objective Listing UI prefab. It does NOT need ObjectiveListing.cs.")]
    public GameObject objectiveListingPrefabFallback = default;

    [Header("UI Containers")]
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

    [Header("Objective Listing Rules")]
    [SerializeField] private int maxObjectivesPerListing = 5;

    private bool isClosed = true;
    private Animator animator;

    private readonly HashSet<ObjectiveListing> runtimeObjectiveListings = new();

    private void OnEnable()
    {
        ObjectiveListing.OnObjectiveListingComplete += UICompleteObjective;
        NextSceneTrigger.NextSceneTriggered += ClearAndRefetchObjectives;
    }

    private void OnDisable()
    {
        ObjectiveListing.OnObjectiveListingComplete -= UICompleteObjective;
        NextSceneTrigger.NextSceneTriggered -= ClearAndRefetchObjectives;
    }

    void Start()
    {
        animator = GetComponent<Animator>();

        ObjectiveListing.ObjectiveToUI.Clear();

        GetObjectiveDependencies();

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
            if (isClosed)
            {
                OpenTracker();
            }
            else
            {
                CloseTracker();
            }
        }
    }

    void GetObjectiveDependencies()
    {
        if (!objectiveListingsHolder)
        {
            objectiveListingsHolder = GameObject.Find("Objective Listings");
        }

        if (!objectiveListingsHolder)
        {
            Debug.LogError("ObjectiveTracker could not find 'Objective Listings' holder.");
            return;
        }

        if (!objectiveListingsUIHolder)
        {
            Debug.LogError("ObjectiveTracker is missing objectiveListingsUIHolder.");
            return;
        }

        objectiveListings.Clear();
        objectiveListingsUI.Clear();

        ObjectiveListing[] sceneListings = objectiveListingsHolder.GetComponentsInChildren<ObjectiveListing>(true);

        foreach (ObjectiveListing listingObject in sceneListings)
        {
            if (!listingObject) continue;
            if (listingObject.isComplete) continue;
            if (CountValidObjectives(listingObject) == 0) continue;

            GameObject prefabToUse = listingObject.objectiveListingPrefab
                ? listingObject.objectiveListingPrefab
                : objectiveListingPrefabFallback;

            if (!prefabToUse)
            {
                Debug.LogError($"No Objective Listing UI prefab assigned for {listingObject.name}, and fallback is missing.");
                continue;
            }

            GameObject listingUI = ObjectiveUIFactory.CreateObjectiveListingUI(
                listingObject,
                prefabToUse,
                objectiveUIPrefab,
                objectiveListingsUIHolder,
                ObjectiveListing.ObjectiveToUI
            );

            if (!listingUI)
            {
                Debug.LogError($"Failed to create UI listing for {listingObject.name}.");
                continue;
            }

            objectiveListings.Add(listingObject);
            objectiveListingsUI.Add(listingUI);
        }

        SetMessyObjectives(messyObjectives);
    }

    private IEnumerator RestorePendingObjectiveStates()
    {
        yield return null;

        PurgeDestroyedReferences();

        var pendingStates = GameManager.Instance?.GetPendingObjectiveStates();
        var pendingNpcStates = GameManager.Instance?.GetPendingNpcStates();

        bool hasObjectiveStates = pendingStates != null && pendingStates.Count > 0;
        bool hasNpcStates = pendingNpcStates != null && pendingNpcStates.Count > 0;

        if (!hasObjectiveStates && !hasNpcStates)
        {
            yield break;
        }

        var restoredTriggers = new HashSet<DialogueTrigger>();

        if (hasNpcStates)
        {
            foreach (var trigger in FindObjectsOfType<DialogueTrigger>())
            {
                var npcState = pendingNpcStates.Find(s => s.npcName == trigger.gameObject.name);

                if (npcState == null || npcState.interactionCount <= 0)
                {
                    continue;
                }

                trigger.SetInteractionCount(npcState.interactionCount);

                if (trigger.ObjectivesToGive != null && trigger.ObjectivesToGive.Count > 0)
                {
                    trigger.MarkObjectiveGiven();
                }

                trigger.InvokeEvents();
                restoredTriggers.Add(trigger);
            }

            GameManager.Instance?.ClearPendingNpcStates();
        }

        var knownKeys = new HashSet<string>();

        foreach (var listing in objectiveListings)
        {
            if (!listing) continue;

            foreach (var obj in listing.objectives)
            {
                if (obj != null)
                {
                    knownKeys.Add(obj.gameObject.name + "|" + obj.description);
                }
            }
        }

        if (hasObjectiveStates)
        {
            foreach (var saved in pendingStates)
            {
                string key = saved.objectiveName + "|" + saved.description;
                bool alreadyInListing = knownKeys.Contains(key);

                foreach (var trigger in FindObjectsOfType<DialogueTrigger>())
                {
                    var toGive = trigger.ObjectivesToGive;

                    if (toGive == null || toGive.Count == 0)
                    {
                        continue;
                    }

                    bool found = toGive.Any(o =>
                        o != null &&
                        o.gameObject.name == saved.objectiveName &&
                        o.description == saved.description
                    );

                    if (!found)
                    {
                        continue;
                    }

                    if (!restoredTriggers.Contains(trigger))
                    {
                        trigger.MarkObjectiveGiven();

                        int count = saved.npcInteractionCount;
                        trigger.SetInteractionCount(count > 0 ? count : 1);

                        trigger.InvokeEvents();
                        restoredTriggers.Add(trigger);
                    }

                    if (!alreadyInListing)
                    {
                        AddObjective(toGive);

                        foreach (var o in toGive)
                        {
                            if (o != null)
                            {
                                knownKeys.Add(o.gameObject.name + "|" + o.description);
                            }
                        }
                    }

                    break;
                }
            }
        }

        int restoredCount = 0;

        if (hasObjectiveStates)
        {
            foreach (var listing in objectiveListings)
            {
                if (!listing) continue;

                for (int i = 0; i < listing.objectives.Count; i++)
                {
                    Objective objective = listing.objectives[i];

                    if (objective == null)
                    {
                        continue;
                    }

                    ObjectiveSaveState saved = pendingStates.Find(
                        s => s.objectiveName == objective.gameObject.name &&
                             s.description == objective.description
                    );

                    if (saved == null)
                    {
                        continue;
                    }

                    objective.RestoreState(saved);

                    if (saved.isComplete)
                    {
                        objective.isComplete = true;
                        restoredCount++;

                        objective.InvokeRestoreEvents();

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
        }

        GameManager.Instance?.SetNumTasksCompleted(restoredCount);

        GameManager.Instance?.ClearPendingObjectiveStates();

        Debug.Log($"Restored {restoredCount} completed objective(s) from pending state.");
    }

    void OpenTracker()
    {
        if (animator)
        {
            animator.SetBool("TrackerOpen", true);
        }

        isClosed = false;
    }

    void CloseTracker()
    {
        if (animator)
        {
            animator.SetBool("TrackerOpen", false);
        }

        isClosed = true;
    }

    private void UICompleteObjective(ObjectiveListing listing)
    {
        if (!listing)
        {
            return;
        }

        PurgeDestroyedReferences();

        int activeListingUICount = objectiveListingsUI.Count(ui => ui);

        // If this is the only visible Objective Listing UI, keep it on screen.
        if (activeListingUICount <= 1)
        {
            Debug.Log("Objective listing completed, but it is the only listing UI present, so it will stay visible.");
            SetMessyObjectives(messyObjectives);
            return;
        }

        int index = objectiveListings.IndexOf(listing);

        if (index < 0)
        {
            return;
        }

        foreach (Objective objective in listing.objectives)
        {
            if (objective != null)
            {
                ObjectiveListing.ObjectiveToUI.Remove(objective);
            }
        }

        GameObject listingUIObject = null;

        if (index < objectiveListingsUI.Count)
        {
            listingUIObject = objectiveListingsUI[index];
        }

        objectiveListings.RemoveAt(index);

        if (index < objectiveListingsUI.Count)
        {
            objectiveListingsUI.RemoveAt(index);
        }

        if (listingUIObject)
        {
            Destroy(listingUIObject);
        }

        listing.objectiveUIList.Clear();

        if (runtimeObjectiveListings.Contains(listing))
        {
            runtimeObjectiveListings.Remove(listing);

            if (listing.gameObject)
            {
                Destroy(listing.gameObject);
            }
        }

        Debug.Log("Destroying completed objective UI listing because more than one listing UI is present.");

        SetMessyObjectives(messyObjectives);
    }

    private void ClearAndRefetchObjectives()
    {
        foreach (GameObject listingUI in objectiveListingsUI.ToList())
        {
            if (listingUI)
            {
                Destroy(listingUI);
            }
        }

        foreach (ObjectiveListing runtimeListing in runtimeObjectiveListings.ToList())
        {
            if (runtimeListing)
            {
                Destroy(runtimeListing.gameObject);
            }
        }

        runtimeObjectiveListings.Clear();
        objectiveListings.Clear();
        objectiveListingsUI.Clear();
        ObjectiveListing.ObjectiveToUI.Clear();

        GetObjectiveDependencies();
    }

    public void SetMessyObjectives(bool set)
    {
        PurgeDestroyedReferences();

        if (objectiveListingsUI == null)
        {
            return;
        }

        if (set)
        {
            int idx = 1;

            foreach (GameObject listing in objectiveListingsUI)
            {
                if (!listing) continue;

                int flip = (idx % 2) == 0 ? -1 : 1;
                listing.transform.rotation = Quaternion.Euler(0f, 0f, messyObjectiveTilt * flip);
                idx++;
            }
        }
        else
        {
            foreach (GameObject listing in objectiveListingsUI)
            {
                if (!listing) continue;

                listing.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }
    }

    public void AddObjective(List<Objective> objectivesToAdd)
    {
        if (objectivesToAdd == null || objectivesToAdd.Count == 0)
        {
            return;
        }

        if (!EnsureRequiredReferences())
        {
            return;
        }

        PurgeDestroyedReferences();

        int addedCount = 0;

        foreach (Objective objective in objectivesToAdd)
        {
            if (!objective) continue;

            if (IsObjectiveAlreadyTracked(objective))
            {
                Debug.LogWarning($"Objective '{objective.description}' is already tracked. Skipping duplicate.");
                continue;
            }

            ObjectiveListing targetListing = GetOrCreateListingWithSpace();

            if (!targetListing)
            {
                Debug.LogError($"Could not create/find an ObjectiveListing for objective '{objective.description}'.");
                continue;
            }

            GameObject targetListingUI = GetUIForListing(targetListing);

            if (!targetListingUI)
            {
                Debug.LogError($"Could not find/create UI for ObjectiveListing '{targetListing.name}'.");
                continue;
            }

            ObjectiveUIFactory.AddToObjectiveToListingUI(
                targetListing,
                new List<Objective> { objective },
                targetListingUI,
                objectiveUIPrefab,
                ObjectiveListing.ObjectiveToUI
            );
            
            // If we add a new incomplete objective to a previously completed listing,
            // the listing must become incomplete again so it can complete later.
            if (!objective.isComplete)
            {
                targetListing.isComplete = false;
            }

            addedCount++;
        }

        if (addedCount > 0)
        {
            Debug.LogWarning($"Added {addedCount} objective(s). Active listing count: {objectiveListings.Count}.");
            SetMessyObjectives(messyObjectives);
        }
    }
    
    private ObjectiveListing GetOrCreateListingWithSpace()
    {
        PurgeDestroyedReferences();

        // Prefer an active, incomplete listing with room.
        foreach (ObjectiveListing listing in objectiveListings)
        {
            if (!listing) continue;

            if (!listing.isComplete && CountValidObjectives(listing) < maxObjectivesPerListing)
            {
                return listing;
            }
        }
        
        // If there is only one listing UI visible, reuse it even if it is complete,
        // as long as it still has room.
        int activeListingUICount = objectiveListingsUI.Count(ui => ui);

        if (activeListingUICount <= 1)
        {
            foreach (ObjectiveListing listing in objectiveListings)
            {
                if (!listing) continue;

                if (CountValidObjectives(listing) < maxObjectivesPerListing)
                {
                    return listing;
                }
            }
        }

        // Otherwise all available listings are full or completed so create a new one.
        return CreateRuntimeObjectiveListing();
    }

    private ObjectiveListing CreateRuntimeObjectiveListing()
    {
        if (!EnsureRequiredReferences())
        {
            return null;
        }

        GameObject runtimeListingObj = new GameObject($"Runtime Objective Listing {objectiveListings.Count + 1}");
        runtimeListingObj.transform.SetParent(objectiveListingsHolder.transform);

        ObjectiveListing runtimeListing = runtimeListingObj.AddComponent<ObjectiveListing>();
        runtimeListing.objectiveListingPrefab = objectiveListingPrefabFallback;

        GameObject listingUI = ObjectiveUIFactory.CreateObjectiveListingUI(
            runtimeListing,
            objectiveListingPrefabFallback,
            objectiveUIPrefab,
            objectiveListingsUIHolder,
            ObjectiveListing.ObjectiveToUI
        );

        if (!listingUI)
        {
            Destroy(runtimeListingObj);
            return null;
        }

        runtimeObjectiveListings.Add(runtimeListing);
        objectiveListings.Add(runtimeListing);
        objectiveListingsUI.Add(listingUI);

        return runtimeListing;
    }

    private GameObject GetUIForListing(ObjectiveListing listing)
    {
        if (!listing)
        {
            return null;
        }

        int index = objectiveListings.IndexOf(listing);

        if (index >= 0 && index < objectiveListingsUI.Count && objectiveListingsUI[index])
        {
            return objectiveListingsUI[index];
        }

        GameObject prefabToUse = listing.objectiveListingPrefab
            ? listing.objectiveListingPrefab
            : objectiveListingPrefabFallback;

        GameObject listingUI = ObjectiveUIFactory.CreateObjectiveListingUI(
            listing,
            prefabToUse,
            objectiveUIPrefab,
            objectiveListingsUIHolder,
            ObjectiveListing.ObjectiveToUI
        );

        if (!listingUI)
        {
            return null;
        }

        if (index >= 0)
        {
            while (objectiveListingsUI.Count <= index)
            {
                objectiveListingsUI.Add(null);
            }

            objectiveListingsUI[index] = listingUI;
        }
        else
        {
            objectiveListings.Add(listing);
            objectiveListingsUI.Add(listingUI);
        }

        return listingUI;
    }

    private bool IsObjectiveAlreadyTracked(Objective objective)
    {
        if (!objective)
        {
            return true;
        }

        foreach (ObjectiveListing listing in objectiveListings)
        {
            if (!listing) continue;

            if (listing.objectives.Contains(objective))
            {
                return true;
            }
        }

        return false;
    }

    private int CountValidObjectives(ObjectiveListing listing)
    {
        if (!listing || listing.objectives == null)
        {
            return 0;
        }

        int count = 0;

        foreach (Objective objective in listing.objectives)
        {
            if (objective)
            {
                count++;
            }
        }

        return count;
    }

    private bool EnsureRequiredReferences()
    {
        if (!objectiveListingsHolder)
        {
            objectiveListingsHolder = GameObject.Find("Objective Listings");
        }

        if (!objectiveListingsHolder)
        {
            Debug.LogError("ObjectiveTracker is missing objectiveListingsHolder and could not find 'Objective Listings'.");
            return false;
        }

        if (!objectiveListingsUIHolder)
        {
            Debug.LogError("ObjectiveTracker is missing objectiveListingsUIHolder.");
            return false;
        }

        if (!objectiveListingPrefabFallback)
        {
            Debug.LogError("ObjectiveTracker is missing objectiveListingPrefabFallback.");
            return false;
        }

        if (!objectiveUIPrefab)
        {
            Debug.LogError("ObjectiveTracker is missing objectiveUIPrefab.");
            return false;
        }

        return true;
    }

    private void PurgeDestroyedReferences()
    {
        for (int i = objectiveListings.Count - 1; i >= 0; i--)
        {
            if (!objectiveListings[i])
            {
                objectiveListings.RemoveAt(i);

                if (i < objectiveListingsUI.Count)
                {
                    objectiveListingsUI.RemoveAt(i);
                }
            }
        }

        for (int i = objectiveListingsUI.Count - 1; i >= 0; i--)
        {
            if (!objectiveListingsUI[i])
            {
                objectiveListingsUI.RemoveAt(i);

                if (i < objectiveListings.Count)
                {
                    objectiveListings.RemoveAt(i);
                }
            }
        }

        runtimeObjectiveListings.RemoveWhere(listing => !listing);
    }
}