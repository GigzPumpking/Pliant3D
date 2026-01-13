using UnityEngine;

    public class FetchObjective : Objective
    {
        private ObjectiveNode ObjectToFetch;
        [SerializeField] private float FetchDistance;
        private Collider2D Collider;
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
            
            InvokeCompletionEvents();
        }
        
        
    }
