using Unity;
using UnityEngine;

[CreateAssetMenu(fileName = "MeditationData", menuName = "ScriptableObjects/MeditationData", order = 1)]
public class PM_MeditationData : ScriptableObject
{
    public float meditateRate;
    public float meditateTime;
    public float meditateZoomPercent;
}