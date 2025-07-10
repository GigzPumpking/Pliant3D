using System;
using Unity;
using UnityEngine;

[CreateAssetMenu(fileName = "MeditationData", menuName = "ScriptableObjects/MeditationData", order = 1)]
public class PM_MeditationData : ScriptableObject
{
    [Tooltip("The percent you want the lockout bar to be at when the player is done meditating.")]
    public float meditateRecoverPercent;
    [Tooltip("How fast you want the meditation to occur.")]
    public float meditateRate;
    [Tooltip("Camera zoom for meditation. If you want no zoom, leave at 1.")]
    public float meditateCameraSizeRatio;
    [Tooltip("Should the player only be allowed to meditate when they're lockout out?")]
    public bool onlyMeditateOnLockout;

    private void Awake()
    {
        if(meditateRecoverPercent > 100f) meditateRecoverPercent = 100;
    }
}