using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectRemovalButton : ButtonScript
{
    public GameObject[] objects;

    public override void OnPress()
    {
        foreach(GameObject obj in objects){
            obj.SetActive(false);
        }
    }

    public override void OnRelease()
    {
        
    }
}