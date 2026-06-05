using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

    /// <summary>
    /// Proxy component that allows FetchObjective to provide dialogue to an alternate return NPC.
    /// This is added at runtime to the alternate NPC's GameObject.
    /// </summary>
    public class FetchObjectiveDialogueProxy : MonoBehaviour, IDialogueProvider
    {
        private FetchObjective sourceObjective;
        private bool useAlternateDialogue;
        
        public void Initialize(FetchObjective objective)
        {
            sourceObjective = objective;
            useAlternateDialogue = true;
        }

        public void Initialize(FetchObjective objective, bool useAlternate)
        {
            sourceObjective = objective;
            useAlternateDialogue = useAlternate;
        }
        
        public int Priority => sourceObjective != null ? sourceObjective.Priority : -1;
        
        public bool HasDialogue => sourceObjective != null && (useAlternateDialogue ? sourceObjective.HasAlternateNPCDialogue() : sourceObjective.HasDialogue);
        
        public DialogueEntry[] GetDialogueEntries() => sourceObjective == null ? null : (useAlternateDialogue ? sourceObjective.GetAlternateNPCDialogueEntries() : sourceObjective.GetDialogueEntries()); 
    }

    public class FetchObjective : Objective, IDialogueProvider
    {
        public static event Action<Objective> OnObjectiveComplete;
        //public static event Action<Objective, int, int> OnItemFetched;
        [SerializeField] List<FetchableInteractable> ObjectsToFetch;
        public DialogueTrigger questGiver;

        [Header("Objective Text")]
        [SerializeField] private string fetchedObjectiveDescription;
        
        [Header("Completion Settings")]
        [Tooltip("If true, player must return to an NPC after fetching all items. If false, quest completes immediately when all items are fetched.")]
        [SerializeField] private bool requiresNPCReturn = true;
        
        [Header("Quest Giver Dialogue")]
        [Tooltip("Shown by quest giver when all items are fetched but not yet returned (fetchedAll == true, isComplete == false)")]
        public DialogueEntry[] itemsReadyDialogue;
        
        [Tooltip("Shown by quest giver after the quest is fully complete (isComplete == true)")]
        public DialogueEntry[] questCompleteDialogue;
        
        [Header("Alternate Return NPC")]
        [Tooltip("Enable to deliver items to a different NPC instead of the quest giver.")]
        [SerializeField] private bool useAlternateNPC = false;
        
        [Tooltip("The NPC to deliver items to (instead of the quest giver).")]
        [SerializeField] private DialogueTrigger alternateNPC;
        
        [Tooltip("Shown by alternate NPC when all items are fetched but not yet returned")]
        public DialogueEntry[] alternateItemsReadyDialogue;
        
        [Tooltip("Shown by alternate NPC after the quest is fully complete")]
        public DialogueEntry[] alternateQuestCompleteDialogue;
        
        // Reference to the proxy added to alternate NPC
        private FetchObjectiveDialogueProxy alternateNPCProxy;
        private FetchObjectiveDialogueProxy questGiverProxy;
        
        // Priority levels for different dialogue states
        private const int PRIORITY_ITEMS_READY = 10;
        private const int PRIORITY_COMPLETE = 20;
        
        //Tally the number of items fetched
        public int numCompleted { get; set; }
        private int cachedTotal;
        
        //reference the objects to fetch/flag if all objects are fetched, then check if npc actually got the object, if so objective complete
        
        //three flags: unfetched, fetched, returned ?
        //if unfetched, when interacted with object -> object is fetched
        //if fetched, when interacted with questGiver -> object is returned
        //if all objects are returned, then the objective is complete

        #region IDialogueProvider Implementation (Quest Giver)
        
        public int Priority
        {
            get
            {
                if (isComplete)
                    return PRIORITY_COMPLETE;
                
                if (fetchedAll)
                    return PRIORITY_ITEMS_READY;
                
                return -1; // No applicable dialogue state, use base dialogue
            }
        }
        
        public bool HasDialogue => HasDialogueForState(itemsReadyDialogue, questCompleteDialogue);
        
        public DialogueEntry[] GetDialogueEntries() => GetDialogueForState(itemsReadyDialogue, questCompleteDialogue);
        
        #endregion
        
        #region Alternate NPC Dialogue Provider Methods
        
        public bool HasAlternateNPCDialogue() => HasDialogueForState(alternateItemsReadyDialogue, alternateQuestCompleteDialogue);
        
        public DialogueEntry[] GetAlternateNPCDialogueEntries() => GetDialogueForState(alternateItemsReadyDialogue, alternateQuestCompleteDialogue);
        
        #endregion
        
        #region Private Helper Methods
        
        /// <summary>
        /// Checks if dialogue exists for the current state given specific dialogue arrays.
        /// </summary>
        private bool HasDialogueForState(DialogueEntry[] readyDialogue, DialogueEntry[] completeDialogue)
        {
            if (isComplete)
                return completeDialogue != null && completeDialogue.Length > 0;
            
            if (fetchedAll)
                return readyDialogue != null && readyDialogue.Length > 0;
            
            return false;
        }
        
        /// <summary>
        /// Gets the dialogue entries for the current state given specific dialogue arrays.
        /// </summary>
        private DialogueEntry[] GetDialogueForState(DialogueEntry[] readyDialogue, DialogueEntry[] completeDialogue)
        {
            if (isComplete)
                return completeDialogue;
            
            if (fetchedAll)
                return readyDialogue;
            
            return null;
        }
        
        #endregion

        private void Awake()
        {
            RefreshCachedTotal();
        }

        private void Start()
        {
            RefreshCachedTotal();

            if(!questGiver) questGiver = GetComponent<DialogueTrigger>();

            EnsureQuestGiverProxy();
            
            // Register this objective as a dialogue provider with the DialogueTrigger
            if (questGiver != null)
            {
                questGiver.RefreshDialogueProviders();
            }
            
            // If there's an alternate return NPC, add a dialogue proxy to it
            if (useAlternateNPC && alternateNPC != null)
            {
                alternateNPCProxy = alternateNPC.gameObject.AddComponent<FetchObjectiveDialogueProxy>();
                alternateNPCProxy.Initialize(this, true);
                alternateNPC.RefreshDialogueProviders();
            }
            
            //UPDATE TALLY AT START (This kinda sucks tho)
            TallyBuilder.InitializeTallyUI(this, cachedTotal);
            RefreshTallyUI();
            UpdateObjectiveDescriptionUI();
        }
        
        private void OnEnable() {
            //subscribe to dialogue trigger event
            //TransformationWheel.TransformedObjective += CheckCompletion;
            DialogueTrigger.InteractedObjective += CheckCompletion;
            EventDispatcher.AddListener<FetchObjectInteract>(OnFetchObjectInteract);
        }

        private void OnDisable() {
            //TransformationWheel.TransformedObjective -= CheckCompletion;
            DialogueTrigger.InteractedObjective -= CheckCompletion;
            EventDispatcher.RemoveListener<FetchObjectInteract>(OnFetchObjectInteract);

            if (questGiverProxy != null)
            {
                Destroy(questGiverProxy);
                questGiverProxy = null;

                if (questGiver != null)
                {
                    questGiver.RefreshDialogueProviders();
                }
            }

            // Clean up the proxy component from alternate NPC
            if (alternateNPCProxy != null)
            {
                Destroy(alternateNPCProxy);
                alternateNPCProxy = null;
                
                // Refresh the alternate NPC's providers
                if (alternateNPC != null)
                {
                    alternateNPC.RefreshDialogueProviders();
                }
            }
        }

        private void OnValidate()
        {
            RefreshCachedTotal();
        }

        private void RefreshCachedTotal()
        {
            int currentTotal = ObjectsToFetch != null ? ObjectsToFetch.Count(obj => obj != null) : 0;

            if (!Application.isPlaying)
            {
                cachedTotal = currentTotal;
                return;
            }

            if (cachedTotal <= 0)
            {
                cachedTotal = currentTotal;
            }
        }

        public void RegisterQuestGiver(DialogueTrigger giver)
        {
            questGiver = giver;
            EnsureQuestGiverProxy();
            RefreshNPCDialogue();
            UpdateObjectiveDescriptionUI();
        }

        private void EnsureQuestGiverProxy()
        {
            if (questGiver == null) return;

            if (questGiver.gameObject != gameObject && questGiverProxy == null)
            {
                questGiverProxy = questGiver.gameObject.AddComponent<FetchObjectiveDialogueProxy>();
                questGiverProxy.Initialize(this, false);
            }

            questGiver.RefreshDialogueProviders();
        }

        private string GetCurrentObjectiveDescription()
        {
            if (fetchedAll && requiresNPCReturn && !string.IsNullOrWhiteSpace(fetchedObjectiveDescription))
                return fetchedObjectiveDescription;

            return description;
        }

        private void UpdateObjectiveDescriptionUI()
        {
            if (!ObjectiveListing.ObjectiveToUI.ContainsKey(this)) return;
            if (ObjectiveListing.ObjectiveToUI[this] == null) return;
            if (ObjectiveListing.ObjectiveToUI[this].DescriptionTXT == null) return;

            string currentDescription = GetCurrentObjectiveDescription();

            if (showTally)
            {
                ObjectiveListing.ObjectiveToUI[this].DescriptionTXT.text = $"{currentDescription} ({numCompleted}/{cachedTotal})";
            }
            else
            {
                ObjectiveListing.ObjectiveToUI[this].DescriptionTXT.text = currentDescription;
            }
        }

        private void OnFetchObjectInteract(FetchObjectInteract e)
        {
            Debug.Log("FetchObjectInteract received in FetchObjective");
            // Check if the fetched object is in the ObjectsToFetch list
            if (ObjectsToFetch.Contains(e.fetchableObject))
            {
                Debug.Log("Object fetched is part of the objective, checking completion...");
                Debug.Log("Object fetched: " + e.fetchableObject.gameObject.name);
                Debug.Log("Is Fetched: " + e.fetchableObject.isFetched);
                
                if(showTally) UpdateTally();
                CheckCompletion();
            }
        }

        private void UpdateTally()
        {
            //TALLY IN UI
            //hey i just grabbed an item (idk who my mom or dad is though)
            if (ObjectiveListing.ObjectiveToUI.ContainsKey(this) == false)
            {
                Debug.LogWarning($"Cannot find UI for this objective {description}!");
                return;
            }

            numCompleted = ObjectsToFetch.Count(obj => obj != null && obj.isFetched);
            numCompleted = Mathf.Clamp(numCompleted, 0, cachedTotal);
            UpdateObjectiveDescriptionUI();
        }

        private void RefreshNPCDialogue()
        {
            EnsureQuestGiverProxy();

            if (alternateNPC != null)
                alternateNPC.RefreshDialogueProviders();
        }

        public bool fetchedAll = false;
        private void CheckCompletion(DialogueTrigger interactedNPC)
        {
            if (!fetchedAll) return;
            if (isComplete) return;

            DialogueTrigger targetNPC = (useAlternateNPC && alternateNPC != null) ? alternateNPC : questGiver;

            if (targetNPC == null) return;
            if (interactedNPC != targetNPC) return;
            
            CompleteObjective();
        }

        private void CheckCompletion()
        {
            //check if all objects are fetched
            foreach(var obj in ObjectsToFetch)
            {
                if (!obj || !obj.isFetched) return;
            }
            
            // Mark that all items have been fetched
            if (!fetchedAll)
            {
                fetchedAll = true;
                RefreshNPCDialogue();
                UpdateObjectiveDescriptionUI();

                // If no NPC return is required, complete immediately
                if (!requiresNPCReturn)
                {
                    CompleteObjective();
                }
                return;
            }

            // This overload doesn't have interact info, so can't complete NPC-return quests here
            // NPC-return completion is handled by the Interact overload
        }
        
        private void CompleteObjective()
        {
            if (isComplete) return; // Prevent double completion
            
            InvokeCompletionEvents();
            isComplete = true;
            RefreshNPCDialogue();
            RefreshTallyUI();
            UpdateObjectiveDescriptionUI();
            OnObjectiveComplete?.Invoke(this);
            Debug.Log("FetchObjective complete!");
        }

        public override ObjectiveSaveState CaptureState()
        {
            var state = base.CaptureState();
            state.fetchedAll = fetchedAll;
            state.numCompleted = numCompleted;
            state.fetchedItemNames = new List<string>();
            foreach (var item in ObjectsToFetch)
            {
                if (item != null && item.isFetched)
                    state.fetchedItemNames.Add(item.gameObject.name);
            }
            return state;
        }

        public override void RestoreState(ObjectiveSaveState state)
        {
            RefreshCachedTotal();

            numCompleted = state.numCompleted;
            fetchedAll = state.fetchedAll;

            // Silently mark previously-fetched items and hide them
            foreach (var item in ObjectsToFetch)
            {
                if (item != null && state.fetchedItemNames.Contains(item.gameObject.name))
                {
                    item.SetFetchedSilently();
                }
            }

            numCompleted = Mathf.Clamp(numCompleted, 0, cachedTotal);

            // Update the tally UI to reflect restored progress
            if (showTally)
                TallyBuilder.UpdateTallyUI(this, numCompleted, cachedTotal);

            RefreshNPCDialogue();
            UpdateObjectiveDescriptionUI();
        }

        public override void RefreshTallyUI()
        {
            RefreshCachedTotal();

            if (showTally)
            {
                TallyBuilder.UpdateTallyUI(this, numCompleted, cachedTotal);
            }
        }
    }