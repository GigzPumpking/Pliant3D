using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveInteract : MonoBehaviour
{
    public bool didInteract;
    public bool formSpecific;
    public Transformation specificTransformation;

    private void Start()
    {
        this.tag = "Objective Interactable"; //ENSURE THAT THIS AT LEAST HAS THE OBJECTIVE INTERACTABLE TAG FOR COLLISION CHECKS
        EventDispatcher.AddListener<ObjectiveInteractEvent>(PlayerInteracted); //LISTENED TO BY 'Objective.cs'
    }

    public void PlayerInteracted(ObjectiveInteractEvent _data)
    {
        //Debug.LogError("Received interact data, gameObject = " + _data.interactedTo.name);
        if(!_data.interactedTo.Equals(this.gameObject)) return; //IF NOT THIS OBJECTIVE, RETURN

        //IF THE OBJECTIVE IS FORM SPECIFC AND THE INTERACT WAS INITIATED BY ANOTHER FORM, RETURN
        if (formSpecific && !(_data.currentTransformation == specificTransformation)) return;
        didInteract = true; //ELSE THE PLAYER DID INTERACT WITH US, RECEIVED FROM 'Player.cs'

        //TELL 'Objective.cs' THAT YOU'VE BEEN INTERACTED WITH
        ObjectiveInteracted objInteracted = new ObjectiveInteracted();
        objInteracted.interactedTo = this.gameObject;
        EventDispatcher.Raise<ObjectiveInteracted>(objInteracted);
    }

}
