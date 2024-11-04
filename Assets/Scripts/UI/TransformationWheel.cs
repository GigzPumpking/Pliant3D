using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformationWheel : MonoBehaviour
{
    public Vector2 normalisedMousePosition;
    public float currentAngle;
    public int selection;
    private int previousSelection;
    
    public GameObject[] transformationItems;
    
    private TransformationItem transformation;
    private TransformationItem previousTransformation;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        normalisedMousePosition = new Vector2(Input.mousePosition.x - Screen.width, 
            Input.mousePosition.y - Screen.height).normalized * 2f;
        currentAngle = Mathf.Atan2(normalisedMousePosition.y, 
            normalisedMousePosition.x) * Mathf.Rad2Deg;

        currentAngle = (currentAngle + 360)%360;

        selection = (int)currentAngle / 45;

        if (selection != previousSelection)
        {
            previousTransformation = transformationItems[previousSelection].GetComponent<TransformationItem>();
            previousSelection.Deselect();
            previousSelection = selection;
            
            transformation = transformationItems[selection].GetComponent<TransformationItem>();
            transformation.Select();
        }
        
        Debug.Log(selection);
    }
}
