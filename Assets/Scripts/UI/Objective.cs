using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class Objective : MonoBehaviour
{
    public enum ObjectiveType
    {
        Location,
        Interact
    }

    [Header("General")]
    public ObjectiveType objectiveType;
    public bool isComplete;
    public string description;

    [Header("Form Specific Settings"), Tooltip("Only applies if the Player is the only item in the Objective Objects List.")]
    public bool formSpecific;
    public Transformation specificTransformation;

    [Header("Objective Dependencies")]
    //OBJECTIVE OBJECTS AND TARGET LOCATIONS
    [SerializeField] List<GameObject> objectiveObjects = new List<GameObject>();
    [SerializeField] GameObject LOC_objectiveLocation; //IF YOU SET THE ENUM TO LOCATION

    [Header("Objective UI")]
    //UI STUFF
    [SerializeField] TextMeshProUGUI ObjectiveDescriptionUI;
    [SerializeField] Animator ObjectiveUIAnimator;
    [SerializeField] GameObject CheckMark;

    //IF YOU SET THE ENUM TO INTERACT
    //[SerializeField] GameObject INT_objective;
    private void Awake()
    {
        if (!ObjectiveDescriptionUI) ObjectiveDescriptionUI = GetComponentInChildren<TextMeshProUGUI>();
        if (!ObjectiveUIAnimator) ObjectiveUIAnimator = GetComponentInChildren<Animator>();
        if(CheckMark != null) CheckMark.SetActive(false);
        InitializeObjects();

        if(!SetDescription()) gameObject.SetActive(false); //IF THE DESCRIPTION OF THE OBJECTIVE IS EMPTY, THEN DISABLE THE OBJECTIVE

        EventDispatcher.AddListener<ReachedTarget>(ObjectReachedTarget); //RAISED FROM 'ObjectiveObject.cs'
        EventDispatcher.AddListener<ObjectiveInteracted>(ObjectiveInteractedWith); //RAISED FROM 'ObjectiveInteract.cs'
    }

    void InitializeObjects()
    {
        //LOCATION OBJECTIVE TYPE
        if (objectiveType == ObjectiveType.Location)
        {
            foreach (GameObject x in objectiveObjects)
            {
                ObjectiveObject obj = x.AddComponent<ObjectiveObject>(); //ADD THE COMPONENT
                obj.targetObject = LOC_objectiveLocation; //SET THE TARGET LOCATION OF THE CURRENT OBJECT

                if(objectiveObjects.Count == 1 && objectiveObjects[0].gameObject.tag == "Player") obj.objectIsPlayer = true;
                obj.formSpecific = this.formSpecific;
                if(formSpecific) obj.specificTransformation = this.specificTransformation;
            }
        }

        //INTERACT OBJECTIVE TYPE
        if(objectiveType == ObjectiveType.Interact)
        {
            foreach (GameObject x in objectiveObjects)
            {
                ObjectiveInteract obj = x.AddComponent<ObjectiveInteract>();
                obj.formSpecific = this.formSpecific;
                if(formSpecific) obj.specificTransformation = this.specificTransformation;
            }
        }
    }
    public void SetDescription(string description)
    {
        //UI STUFF
        this.description = description;
        ObjectiveDescriptionUI.SetText(description);
    }

    bool SetDescription()
    {
        if (description == string.Empty) return false;
        ObjectiveDescriptionUI.SetText(description); return true;
    }

    public void ObjectReachedTarget(ReachedTarget _data) 
    {
        if (!objectiveObjects.Contains(_data.obj))
        {
            //Debug.LogError(this.gameObject.name + " does NOT contain the object that just got raised");
            return; //MAKE SURE THAT THE EVENT RECEIVED WAS FROM AN OBJECT THAT IS IN THIS OBJECTIVE
        }
        CheckCompletionLocation();
    }

    public void ObjectiveInteractedWith(ObjectiveInteracted _data)
    {
        //Debug.LogError("Did receive interact data: " + _data.interactedTo.name);
        if (!objectiveObjects.Contains(_data.interactedTo))
        {
            //Debug.LogError(this.gameObject.name + " does NOT contain the object that just got raised");
            return; //MAKE SURE THAT THE EVENT RECEIVED WAS FROM AN OBJECT THAT IS IN THIS OBJECTIVE
        }
        CheckCompletionInteract();
    }

    void CheckCompletionInteract()
    {
        bool allInteracted = false;
        foreach (GameObject x in objectiveObjects)
        {
            if (!x.TryGetComponent<ObjectiveInteract>(out ObjectiveInteract obj))
            {
                //Debug.LogError("Couldnt grab component from " + x.gameObject.name);
                return; //IF YOU CANT GRAB THE OBJECTIVE OBJECT COMPONENT, THEN RETURN
            }
            if (!obj.didInteract)
            {
                //Debug.LogError(x.gameObject.name + " has not reached their target.");
                return; //IF ANY OBJECTS WERE NOT INTERACTED WITH, THEN RETURN
            }
        }
        //IF THEY ALL REACHED IT, THEN THE OBJECTIVE IS COMPLETE
        allInteracted = true;
        if (allInteracted) SetCompletion(true);
    }

    void CheckCompletionLocation()
    {
        bool allReached = false;
        foreach (GameObject x in objectiveObjects)
        {
            if (!x.TryGetComponent<ObjectiveObject>(out ObjectiveObject obj))
            {
                //Debug.LogError("Couldnt grab component from " + x.gameObject.name);
                return; //IF YOU CANT GRAB THE OBJECTIVE OBJECT COMPONENT, THEN RETURN
            }
            if (!obj.reachedTarget)
            {
                //Debug.LogError(x.gameObject.name + " has not reached their target.");
                return; //IF ANY OBJECTS DID NOT REACH THEIR LOCATION, THEN RETURN
            }
        }
        //IF THEY ALL REACHED IT, THEN THE OBJECTIVE IS COMPLETE
        allReached = true;
        if (allReached) SetCompletion(true);
    }

    void SetCompletion(bool set)
    {
        if (set)
        {
            Debug.LogWarning($"Objective of description: [{description}] successfully completed.");
            if (CheckMark != null) CheckMark.SetActive(true);
            isComplete = set;
        }
    }
}
