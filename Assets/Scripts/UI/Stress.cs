using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Stress : MonoBehaviour
{

    #region PROPERTIES

    /// <summary>
        /// The Stress damage over time and ability damage components
    /// </summary>
    [Header("CONFIGURATIONS")]
    [Tooltip("The current stress amount.")]
    [SerializeField] private float stress = 0;
    [Tooltip("The max stress limit before debuffed.")]
    [SerializeField] private float maxStress = 100f;

    [Tooltip("The additional damage taken for using an ability.")]
    [SerializeField] private float additonalStress = 5f;
    [Tooltip("The amount damage taken over time for not being in base form.")]
    [SerializeField] private float SOTAmount = 3.3f;
    [Tooltip("The tick speed for damage over time.")]
    [SerializeField] private float TickSpeed = 1f;
    private float currentTickTime = 0f;
    private float nextTickTime = 0f;

    [Space(8f)]
    [Header("IMAGES")]
    /// <summary>
    /// The images and sprites for stress stages and meter fill.
    /// </summary>
    //public StressStage stressStage { get; private set; }

    // public Image stressMeterR;
    // public Image stressMeterL;

    [Tooltip("The filling image for the stress meter.")]
    public Image stressCircle;

    //[SerializeField] private Sprite[] healthSprites;
    //public Image stressStageImage;

    #endregion

    #region PRIVATE METHODS

    void Start()
    {
        // Possibly redundant can maybe remove if uneccessary.
        EventDispatcher.RemoveListener<StressAbility>(StressAbilityHandler);

        EventDispatcher.AddListener<StressAbility>(StressAbilityHandler);

        EventDispatcher.AddListener<Heal>(HealHandler);
    }

    // Update is called once per frame
    void Update()
    {
        StressHandler();

        //Adjust meter UI according to stress amount
        // stressMeterR.fillAmount = Mathf.Clamp((stress/maxStress), 0, 1);
        // stressMeterL.fillAmount = Mathf.Clamp((stress/maxStress), 0, 1);
        stressCircle.fillAmount = Mathf.Clamp((stress/maxStress), 0, 1);

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
            //UpdateStress();
        }
    }

    void StressAbilityHandler(StressAbility e)
    {
        stress += additonalStress;
    }

// Reactivate this function if stress stage image/progression is reimplemented

    // private void UpdateStress()
    // {
    //     // if stress is 75% or more
    //     if( stress >= maxStress*3/4)
    //     {
    //         stressStageImage.sprite = healthSprites[3];
    //         stressStageImage.SetNativeSize();
    //     }// 50%
    //     else if( stress >= maxStress/2)
    //     {
    //         stressStageImage.sprite = healthSprites[2];
    //         stressStageImage.SetNativeSize();
    //     }// 25%
    //     else if( stress >= maxStress/4)
    //     {
    //         stressStageImage.sprite = healthSprites[1];
    //         stressStageImage.SetNativeSize();
    //     }
    //     else
    //     { // 0%
    //         stressStageImage.sprite = healthSprites[0];
    //         stressStageImage.SetNativeSize();
    //     }
    // }

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

    void HealHandler(Heal e) {
        StartCoroutine(ResetStress());
    }

    #endregion

}
