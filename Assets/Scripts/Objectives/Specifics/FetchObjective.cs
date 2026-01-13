using System.Collections.Generic;
using UnityEngine;

    public class FetchObjective : Objective
    {
        private List<FetchableObject> ObjectsToFetch;

        private DialogueTrigger questGiver;
        //reference the objects to fetch/flag if all objects are fetched, then check if npc actually got the object, if so objective complete
        
        //three flags: unfetched, fetched, returned ?
        //if unfetched, when interacted with object -> object is fetched
        //if fetched, when interacted with questGiver -> object is returned
        //if all objects are returned, then the objective is complete

        private void Start()
        {
            if(!questGiver) questGiver = GetComponent<DialogueTrigger>();
        }
        
        private void OnEnable() {
            //subscribe to dialogue trigger event
            //TransformationWheel.TransformedObjective += CheckCompletion;
            EventDispatcher.AddListener<Interact>(CheckCompletion);
        }

        private void OnDisable() {
            //TransformationWheel.TransformedObjective -= CheckCompletion;
            EventDispatcher.RemoveListener<Interact>(CheckCompletion);
        }
        
        private void CheckCompletion(Interact interact)
        {
            //check if the interact raised was by the player interacting with the questGiver. Mark all objects in 'inventory' as 'returned' at this point IF it was 'fetched'
            if(interact.)
            for(FetchableObject fetchable : ObjectsToFetch)
            {
                if (fetchable.isFetched)
                {
                    ObjectsToFetch.Remove(fetchable);
                }
            }
            
            //check if all objects are 'returned'
            //if so then invoke completion events
            if(ObjectsToFetch.Count == 0)
            
            InvokeCompletionEvents();
        }
    }
