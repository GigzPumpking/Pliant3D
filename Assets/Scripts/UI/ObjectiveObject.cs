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
    public bool objectIsPlayer;
    public bool formSpecific;
    public Transformation specificTransformation;

    private void OnTriggerEnter(Collider other)
    {
        //Debug.LogError(this.gameObject.name + " has collided with: " + other.gameObject.name);
        if(objectIsPlayer && formSpecific)
        {
            //NEST SO WE DONT CALL TO STATIC INSTANCE UNLESS THESE TWO ARE TRUE
            if(Player.Instance.transformation == specificTransformation)
            {
                if (other.gameObject.Equals(targetObject))
                {
                    reachedTarget = true;
                    ReachedTarget _data = new ReachedTarget();
                    _data.obj = this.gameObject;
                    EventDispatcher.Raise<ReachedTarget>(_data);
                }
            }
        }
        //IF YOU'RE NOT FORM SPECIFIC, AND THE OBJECT ISN'T THE PLAYER, CHECK INTERACTS NORMALLY
        else if(other.gameObject.Equals(targetObject))
        {
            reachedTarget = true;
            ReachedTarget _data = new ReachedTarget();
            _data.obj = this.gameObject;
            EventDispatcher.Raise<ReachedTarget>(_data);
        }
    }
}
