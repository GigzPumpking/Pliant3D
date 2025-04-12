using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Objective : MonoBehaviour
{
    public enum ObjectiveType
    {
        Location,
        Interact
    }

    public bool isComplete {get; private set; } = false;
    public ObjectiveType objectiveType;
    public string description { get; private set; }

    [SerializeField] List<GameObject> objectiveObjects = new List<GameObject>();
    //IF YOU SET THE ENUM TO LOCATION
    [SerializeField] GameObject LOC_objectiveLocation;

    //IF YOU SET THE ENUM TO INTERACTg
    //[SerializeField] GameObject LOC_objectiveLocation;


    private void Awake()
    {
        ObjectiveTracker.Instance.AddToMap(this);
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (objectiveType == ObjectiveType.Location)
        {
            if (other.gameObject != LOC_objectiveLocation) return;
            else ObjectiveTracker.Instance.CompleteTask(this);
        }
    }

    public void SetDescription(string description)
    {
        this.description = description;
    }

    public void SetCompletion(bool set)
    {
        isComplete = set;
    }
}
