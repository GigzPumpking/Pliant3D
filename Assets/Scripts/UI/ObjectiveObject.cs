using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectiveObject : MonoBehaviour
{
    public enum ObjectType
    {
        Target,
        Object
    }

    public ObjectType type;
    public GameObject targetObject; //LEAVE NULL IF THIS IS A TARGET LOCATION
    public bool reachedTarget;
    static ObjectiveTracker tracker;
    private void Start()
    {
        tracker = ObjectiveTracker.Instance;
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.LogError(this.gameObject.name + " has collided with: " + other.gameObject.name);
        if(other.gameObject == targetObject)
        {
            reachedTarget = true;
            ReachedTarget _data = new ReachedTarget();
            _data.obj = this.gameObject;
            EventDispatcher.Raise<ReachedTarget>(_data);
        }
    }
}
