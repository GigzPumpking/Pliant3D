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
        
        public void Initialize(FetchObjective objective)
        {
            sourceObjective = objective;
        }
        
        public int Priority => sourceObjective != null ? sourceObjective.Priority : -1;
        
        public bool HasDialogue => sourceObjective != null && sourceObjective.HasAlternateNPCDialogue();
        
        public DialogueEntry[] GetDialogueEntries() => sourceObjective?.GetAlternateNPCDialogueEntries(); 
    }

    public class FetchObjective : Objective, IDialogueProvider
    {
        public static event Action<Objective> OnObjectiveComplete;
        //public static event Action<Objective, int, int> OnItemFetched;
        [SerializeField] List<FetchableInteractable> ObjectsToFetch;
        private DialogueTrigger questGiver;
        
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
        
        // Priority levels for different dialogue states
        private const int PRIORITY_ITEMS_READY = 10;
        private const int PRIORITY_COMPLETE = 20;
        
        //Tally the number of items fetched
        public int numCompleted { get; set; }
        
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

        private void Start()
        {
            if(!questGiver) questGiver = GetComponent<DialogueTrigger>();
            
            // Register this objective as a dialogue provider with the DialogueTrigger
            if (questGiver != null)
            {
                questGiver.RefreshDialogueProviders();
            }
            
            // If there's an alternate return NPC, add a dialogue proxy to it
            if (useAlternateNPC && alternateNPC != null)
            {
                alternateNPCProxy = alternateNPC.gameObject.AddComponent<FetchObjectiveDialogueProxy>();
                alternateNPCProxy.Initialize(this);
                alternateNPC.RefreshDialogueProviders();
            }
            
            //UPDATE TALLY AT START (This kinda sucks tho)
            TallyBuilder.InitializeTallyUI(this, ObjectsToFetch.Count);
        }
        
        private void OnEnable() {
            //subscribe to dialogue trigger event
            //TransformationWheel.TransformedObjective += CheckCompletion;
            EventDispatcher.AddListener<Interact>(CheckCompletion);
            EventDispatcher.AddListener<FetchObjectInteract>(OnFetchObjectInteract);
        }

        private void OnDisable() {
            //TransformationWheel.TransformedObjective -= CheckCompletion;
            EventDispatcher.RemoveListener<Interact>(CheckCompletion);
            EventDispatcher.RemoveListener<FetchObjectInteract>(OnFetchObjectInteract);
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

        private void OnFetchObjectInteract(FetchObjectInteract e)
        {
            Debug.Log("FetchObjectInteract received in FetchObjective");
            // Check if the fetched object is in the ObjectsToFetch list
            if (ObjectsToFetch.Contains(e.fetchableObject))
            {
                Debug.Log("Object fetched is part of the objective, checking completion...");
                Debug.Log("Object fetched: " + e.fetchableObject.gameObject.name);
                Debug.Log("Is Fetched: " + e.fetchableObject.isFetched);
                
                UpdateTally();
                CheckCompletion();
            }
        }

        private void UpdateTally()
        {
            //TALLY IN UI
            //hey i just grabbed an item (idk who my mom or dad is though)
            if (ObjectiveListing.ObjectiveToUI.ContainsKey(this) == false)
            {
                Debug.LogError($"Cannot find UI for this objective {description}!");
                return;
            }

            TallyBuilder.UpdateTallyUI(this, ++numCompleted, ObjectsToFetch.Count);
        }

        public bool fetchedAll = false;
        private void CheckCompletion(Interact interact)
        {
            foreach(var obj in ObjectsToFetch)
            {
                if (!obj.isFetched) return;
            }
            
            // Mark that all items have been fetched
            if (!fetchedAll)
            {
                fetchedAll = true;

                // If no NPC return is required, complete immediately
                if (!requiresNPCReturn)
                {
                    CompleteObjective();
                }
                return;
            }

            // Determine which NPC to check for return
            DialogueTrigger targetNPC = (useAlternateNPC && alternateNPC != null) ? alternateNPC : questGiver;
            
            //check if the interact raised was by the player interacting with the correct NPC
            if (interact.questGiver != targetNPC) return;
            
            CompleteObjective();
        }

        private void CheckCompletion()
        {
            //check if all objects are fetched
            foreach(var obj in ObjectsToFetch)
            {
                if (!obj.isFetched) return;
            }
            
            // Mark that all items have been fetched
            if (!fetchedAll)
            {
                fetchedAll = true;

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
            OnObjectiveComplete?.Invoke(this);
            Debug.Log("FetchObjective complete!");
        }
    }
