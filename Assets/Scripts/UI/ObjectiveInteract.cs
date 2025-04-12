using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveInteract : MonoBehaviour
{
    public bool didInteract;
    public bool formSpecific;

    private void Start()
    {
        EventDispatcher.AddListener<ObjectiveInteractEvent>(PlayerInteracted);
    }

    public void PlayerInteracted(ObjectiveInteractEvent _data)
    {
        //Debug.LogError("Received interact data, gameObject = " + _data.interactedTo.name);
        if (!_data.interactedTo.Equals(this.gameObject)) return; //IF NOT THIS OBJECTIVE, RETURN
        didInteract = true; //ELSE THE PLAYER DID INTERACT WITH US, RECEIVED FROM 'Player.cs'

        //TELL 'Objective.cs' THAT YOU'VE BEEN INTERACTED WITH
        ObjectiveInteracted objInteracted = new ObjectiveInteracted();
        objInteracted.interactedTo = this.gameObject;
        EventDispatcher.Raise<ObjectiveInteracted>(objInteracted);
    }

}
