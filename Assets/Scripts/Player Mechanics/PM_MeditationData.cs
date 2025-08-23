using System;
using Unity;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "MeditationData", menuName = "ScriptableObjects/MeditationData", order = 1)]
public class PM_MeditationData : ScriptableObject
{
    [FormerlySerializedAs("meditateRecoverPercent")] [Tooltip("How many charges you want to recover with meditation.")]
    public float meditateRecoverChargesAmt;
    [Tooltip("Camera zoom for meditation. If you want no zoom, leave at 1.")]
    public float meditateCameraSizeRatio;
    [Tooltip("Should the player only be allowed to meditate when they're lockout out?")]
    public bool onlyMeditateOnLockout;
    public float timeForMeditate;

    private void Awake()
    {
        if(meditateRecoverChargesAmt > 4f) meditateRecoverChargesAmt = 4;
    }
}