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
    private float nextTickTime = 0f;
    public StressStage stressStage { get; private set; }

    public Image stressMeterR;
    public Image stressMeterL;
    [SerializeField] private Sprite[] healthSprites;
    public Image stressStageImage;

    // Update is called once per frame
    void Update()
    {
        StressHandler();

        //Adjust meter UI according to stress amount
        stressMeterR.fillAmount = Mathf.Clamp((stress/maxStress), 0, 1);
        stressMeterL.fillAmount = Mathf.Clamp((stress/maxStress), 0, 1);

        if (stress >= maxStress)
        {
            Debug.Log("Max stress Reached debuff active.");
            // lerp stress to 0 over 1 second in a coroutine
            StartCoroutine(ResetStress());

            EventDispatcher.Raise<StressDebuff>(new StressDebuff());
        }

        if (stress <= 0)
        {
            stress = 0;
        }

    }

    void StressHandler()
    {
        currentTickTime += Time.deltaTime;

        if (currentTickTime > nextTickTime)
        {
            nextTickTime += TickSpeed;
            if (Player.Instance.GetTransformation() != Transformation.TERRY) {
                stress += SOTAmount;
            } else {
                stress -= SOTAmount*3/4;
            }
            UpdateStress();
        }
    }

    private void UpdateStress()
    {
        // if stress is 75% or more
        if( stress >= maxStress*3/4)
        {
            stressStageImage.sprite = healthSprites[3];
            stressStageImage.SetNativeSize();
        }// 50%
        else if( stress >= maxStress/2)
        {
            stressStageImage.sprite = healthSprites[2];
            stressStageImage.SetNativeSize();
        }// 25%
        else if( stress >= maxStress/4)
        {
            stressStageImage.sprite = healthSprites[1];
            stressStageImage.SetNativeSize();
        }
        else
        { // 0%
            stressStageImage.sprite = healthSprites[0];
            stressStageImage.SetNativeSize();
        }
    }

    IEnumerator ResetStress()
    {
        float time = 1f;
        float elapsedTime = 0f;
        float startStress = stress;
        while (elapsedTime < time)
        {
            stress = Mathf.Lerp(startStress, 0, elapsedTime/time);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

}
