using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ObjectiveGrouping
{
    TUTORIAL,
    JERRYS_DESK,
    MERIS_FILES,
    PERRYS_EXTRA,
    CARRIE_PC
}
public class ObjectiveGroup : MonoBehaviour
{
    public ObjectiveGrouping grouping;
    public List<GameObject> Objectives = new List<GameObject>();
    private void Start()
    {
        //UPON AWAKE, THIS WILL AUTOMATICALLY BE ADDED TO THE OBJECTIVE TRACKER
        AddChildrenToList();
        ObjectiveTracker.Instance.ObjectiveGroups.Add(this.gameObject);
    }
    void AddChildrenToList()
    {
        //EACH CHILD IN THIS GAMEOBJECT WILL BE CONSIDERED A UI ELEMENT WITH A CORRESPONDING OBJECTIVE
        foreach(Objective obj in GetComponentsInChildren<Objective>(true))
        {
            if(!Objectives.Contains(obj.gameObject)) Objectives.Add(obj.gameObject);
            obj.gameObject.SetActive(true);
        }
    }
}
