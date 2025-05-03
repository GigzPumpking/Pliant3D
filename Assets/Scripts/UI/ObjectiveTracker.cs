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
    // public GameObject objectiveParent;
    //public GameObject[] objectiveUIList;
    public Dictionary<Objective, ObjectiveUI> ObjectiveTable = new Dictionary<Objective, ObjectiveUI>();
    public List<GameObject> ObjectiveGroups = new List<GameObject>(); //HOLDING OF ALL THE OBJECTIVE GROUPS
    public GameObject openObjectiveGroup; //HOLDING OF THE OPEN OBJECTIVE GROUP
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
        openObjectiveGroup?.SetActive(true);
    }

    void CloseTracker()
    {
        animator.SetBool("TrackerOpen", false);
        openObjectiveGroup?.SetActive(false);
    }

    //public void AddToMap(Objective _data)
    //{
    //    if (_data == null) return;

    //    Debug.LogWarning($"Objective of description: {_data.description} added to the objective tracker.");
    //    GameObject objUIObject = GameObject.Instantiate(ObjectiveUIPrefab, this.gameObject.transform); //figure out how to instantiate this in correct UI formatting
    //    bool add = objUIObject.TryGetComponent<ObjectiveDescriptionUI>(out ObjectiveDescriptionUI objUI);
    //    objUI.SetDescription(_data.description);

    //    //NULL CHECK
    //    if (!add)
    //    {
    //        Debug.LogWarning("Objective of Description: " + _data.description + " was unsuccessfully given a corresponding Objective UI.");
    //        return;
    //    }

    //    //IF NEITHER WERE NULL, THEN ADD TO THE MAP
    //    ObjectiveTable.Add(_data, objUI);
    //    //ObjectivesGameObjects.Add(_data.gameObject);
    //}

    public void MovedObjectToTarget()
    {

    }

    //public void CompleteTask(Objective _data)
    //{
    //    _data.SetCompletion(true); //SET THE OBJECTIVE DATA TO COMPLETE
    //    //ObjectiveTable[_data].CompleteTask(); //PLAY THE OBJECTIVE UI COMPLETION
    //}

    //public void AutoCompleteAllTasks()
    //{
    //    foreach (var objectiveTask in ObjectiveTable)
    //    {
    //        var objective = objectiveTask.Key.GetComponent<Objective>();
    //        objective.SetCompletion(true);
    //    }
    //}
}
