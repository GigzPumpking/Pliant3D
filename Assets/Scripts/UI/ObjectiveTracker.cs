using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ObjectiveTracker : MonoBehaviour
{
    private bool isClosed = true;
    private Animator animator;
    // public GameObject objectiveParent;
    public GameObject[] objectiveList;
    
    
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            switch (isClosed)
            {
                case true:
                    OpenTracker();
                    isClosed = false;
                    break;
                case false:
                    CloseTracker();
                    isClosed = true;
                    break;
            }
        }
    }

    void OpenTracker()
    {
        animator.SetBool("TrackerOpen", true);
    }

    void CloseTracker()
    {
        animator.SetBool("TrackerOpen", false);
    }

    public void CompleteTask()
    {
        foreach (var objectiveTask in objectiveList)
        {
            var objective = objectiveTask.GetComponent<Objective>();
            
            if (!objective.isComplete)
            {
                objective.CompleteTask();
                break;
            }
        }
    }
}
