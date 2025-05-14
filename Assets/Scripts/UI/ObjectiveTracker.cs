using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class ObjectiveTracker : MonoBehaviour
{
    private bool isClosed = true;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            switch (isClosed)
            {
                case true:
                    OpenTracker();
                    break;
                case false:
                    CloseTracker();
                    break;
            }
        }
    }

    void OpenTracker()
    {
        animator.SetBool("TrackerOpen", true);
        isClosed = false;
    }

    void CloseTracker()
    {
        animator.SetBool("TrackerOpen", false);
        isClosed = true;
    }
}
