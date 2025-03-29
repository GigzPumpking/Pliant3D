using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class ObjectiveTracker : MonoBehaviour
{
    private bool isClosed = true;
    private Animator animator;
    // public GameObject objectiveParent;
    //public GameObject[] objectiveUIList;
    public Dictionary<Objective, ObjectiveUI> ObjectiveTable = new Dictionary<Objective, ObjectiveUI>();

    //SINGLETON
    private static ObjectiveTracker instance;
    public static ObjectiveTracker Instance { get { return instance; } }

    [SerializeField] GameObject ObjectiveUIPrefab;

    private void Awake()
    {
        if (instance == null) instance = this;
        else
        {
            Destroy(this.gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

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

    public void AddToMap(Objective _data)
    {
        if (_data == null) return;
        GameObject objUIObject = GameObject.Instantiate(ObjectiveUIPrefab);
        bool add = objUIObject.TryGetComponent<ObjectiveUI>(out ObjectiveUI objUI);
        objUI.SetDescription(_data.description);

        //NULL CHECK
        if (!add)
        {
            Debug.LogWarning("Objective of Description: " + _data.description + " was unsuccessfully given a corresponding Objective UI.");
            return;
        }

        //IF NEITHER WERE NULL, THEN ADD TO THE MAP
        ObjectiveTable.Add(_data, objUI);
    }

    public void CompleteTask(Objective _data)
    {
        //foreach (var objectiveTask in objectiveList)
        //{
        //    var objective = objectiveTask.GetComponent<Objective>();

        //    if (!objective.isComplete)
        //    {
        //        objective.CompleteTask();
        //        break;
        //    }
        //}
        _data.SetCompletion(true); //SET THE OBJECTIVE DATA TO COMPLETE
        ObjectiveTable[_data].CompleteTask(); //PLAY THE OBJECTIVE UI COMPLETION
    }
}
