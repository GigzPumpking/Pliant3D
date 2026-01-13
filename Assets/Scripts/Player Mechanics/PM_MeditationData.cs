using System;
using Unity;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Video;

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
    [Tooltip("Should the meditation 'pause' length be tied to the length of the video?")]
    public bool tieMeditateTimeToMeditationClip;
    
    [Tooltip("This is the video overlay that will appear upon meditating")]
    public VideoClip meditationClip;

    private void Awake()
    {
        if(meditateRecoverChargesAmt > 4f) meditateRecoverChargesAmt = 4;
    }

    private void OnValidate()
    {
        if (tieMeditateTimeToMeditationClip) timeForMeditate = (float)meditationClip.length;
    }
}