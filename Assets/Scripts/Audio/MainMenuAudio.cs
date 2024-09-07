using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuAudio : MonoBehaviour
{
    public void PlayBackground()
    {
        AudioManager.Instance.Play("Main Menu BGM");
        AudioManager.Instance.Play("Main Menu Ambience");
    }

    public void StopBackgorund()
    {
        AudioManager.Instance.Stop("Main Menu BGM");
        AudioManager.Instance.Stop("Main Menu Ambience");
    }

    public void DingPlay()
    {
        AudioManager.Instance.Play("Main Menu Play");
    }

    public void CrumplePlay()
    {
        AudioManager.Instance.Play("Crumple Select");
    }
}
