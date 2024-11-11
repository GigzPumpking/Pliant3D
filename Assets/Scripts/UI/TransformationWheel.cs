using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TransformationWheel : MonoBehaviour
{
    public Vector2 normalisedMousePosition;
    public float currentAngle;
    public int hoveredSelection;
    [SerializeField] private int previousHover;
    private int numOfSelection = 4;
    
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
        //Radial unit circle based off of screen and mouse position
        normalisedMousePosition = new Vector2(Input.mousePosition.x - Screen.width/2, 
            Input.mousePosition.y - Screen.height/2);
        
        // Because wheel is rotated add 45 to offset angle. Remove 45 at end if changed
        currentAngle = Mathf.Atan2(normalisedMousePosition.y, 
            normalisedMousePosition.x) * Mathf.Rad2Deg + 45;
        
        //bind angle to between 0 and 360 and clamp range between 0 and 359 to 
        currentAngle = Mathf.Clamp((currentAngle + 360)%360, 0, 359);
        
        //create index based off section of wheel over the number of selections
        hoveredSelection = (int)currentAngle/(360/numOfSelection);
        
        // if hovered isn't the same as previous hovered then selection is made
        if (hoveredSelection != previousHover)
        {
            //activate previous hover's animation
            previousTransformation = transformationItems[previousHover].GetComponent<TransformationItem>();
            previousTransformation.HoverExit();
            previousHover = hoveredSelection;
            
            //activate current hover's animaton.
            transformation = transformationItems[hoveredSelection].GetComponent<TransformationItem>();
            transformation.HoverEnter();
        }
        
        Debug.Log(hoveredSelection);
    }
}
