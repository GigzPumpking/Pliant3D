
using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
    public static bool LoadingScreenActive { get; set; }

    public void OnEnable()
    {
        LoadingScreenActive = true;
    }

    public void OnDisable()
    {
        LoadingScreenActive = false;
    }
}


