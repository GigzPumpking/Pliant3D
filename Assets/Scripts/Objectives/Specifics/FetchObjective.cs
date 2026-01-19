using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

    public class FetchObjective : Objective, IDialogueProvider
    {
        public static event Action<Objective> OnObjectiveComplete;
        [SerializeField] List<FetchableInteractable> ObjectsToFetch;
        private DialogueTrigger questGiver;
        
        [Header("Completion Settings")]
        [Tooltip("If true, player must return to an NPC after fetching all items. If false, quest completes immediately when all items are fetched.")]
        [SerializeField] private bool requiresNPCReturn = true;
        
        [Tooltip("Optional: If set, player must deliver items to this NPC instead of the quest giver. Leave empty to return to quest giver.")]
        [SerializeField] private DialogueTrigger alternateReturnNPC;
        
        [Header("Quest Dialogue")]
        [Tooltip("Shown when all items are fetched but not yet returned (fetchedAll == true, isComplete == false)")]
        public DialogueEntry[] itemsReadyDialogue;
        
        [Tooltip("Shown after the quest is fully complete (isComplete == true)")]
        public DialogueEntry[] questCompleteDialogue;
        
        // Priority levels for different dialogue states
        private const int PRIORITY_ITEMS_READY = 10;
        private const int PRIORITY_COMPLETE = 20;
        
        //reference the objects to fetch/flag if all objects are fetched, then check if npc actually got the object, if so objective complete
        
        //three flags: unfetched, fetched, returned ?
        //if unfetched, when interacted with object -> object is fetched
        //if fetched, when interacted with questGiver -> object is returned
        //if all objects are returned, then the objective is complete

        #region IDialogueProvider Implementation
        
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
        
        public bool HasDialogue
        {
            get
            {
                if (isComplete)
                    return questCompleteDialogue != null && questCompleteDialogue.Length > 0;
                
                if (fetchedAll)
                    return itemsReadyDialogue != null && itemsReadyDialogue.Length > 0;
                
                return false;
            }
        }
        
        public DialogueEntry[] GetDialogueEntries()
        {
            if (isComplete)
                return questCompleteDialogue;
            
            if (fetchedAll)
                return itemsReadyDialogue;
            
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
                CheckCompletion();
            }
        }

        public bool fetchedAll = false;
        private void CheckCompletion(Interact interact)
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
                    return;
                }
                return;
            }

            // Determine which NPC to check for return
            DialogueTrigger targetNPC = alternateReturnNPC != null ? alternateReturnNPC : questGiver;
            
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
                    return;
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
