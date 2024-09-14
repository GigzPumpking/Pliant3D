using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Stress : MonoBehaviour
{
    [SerializeField] private float stress = 0;
    public float maxStress { get; private set; } = 100f;

    [SerializeField] private float SOTAmount = 3.3f;
    [SerializeField] private float TickSpeed = 1f;
    private float currentTickTime = 0f;
    private float nextTickTIme = 0f;

    private Player player;
    public StressStage stressStage { get; private set; }

    public Image stressMeterR;
    public Image stressMeterL;
    [SerializeField] private Sprite[] healthSprites;
    public Image stressStageImage;

    // Start is called before the first frame update
    void Start()
    {
        player = this.GetComponentInParent<Player>();
    }

    // Update is called once per frame
    void Update()
    {
        //if player isn't in Terry form increase stress
        if (player.GetTransformation() != Transformation.TERRY)
        {
            StressHandler();
        }

        //Adjust meter UI according to stress amount
        stressMeterR.fillAmount = Mathf.Clamp((stress/maxStress), 0, 1);
        stressMeterL.fillAmount = Mathf.Clamp((stress/maxStress), 0, 1);

        if (stress >= maxStress)
        {
            Debug.Log("Max stress Reached debuff active.");
        }
        
        if (stress > maxStress)
        {
            stress = maxStress;
        }

    }

    void StressHandler ()
    {
        currentTickTime += Time.deltaTime;

        if (player.GetTransformation() != Transformation.TERRY && 
            currentTickTime > nextTickTIme)
        {
            nextTickTIme += TickSpeed;
            stress += SOTAmount;
            UpdateStress();
        }
    }

    private void UpdateStress()
    {
        // if stress is 75% or more
        if( stress >= maxStress*3/4)
        {
            stressStageImage.sprite = healthSprites[3];
        }// 50%
        else if( stress >= maxStress/2)
        {
            stressStageImage.sprite = healthSprites[2];
        }// 25%
        else if( stress >= maxStress/4)
        {
            stressStageImage.sprite = healthSprites[1];
        }
        else
        { // 0%
            stressStageImage.sprite = healthSprites[0];
        }
    }

}
