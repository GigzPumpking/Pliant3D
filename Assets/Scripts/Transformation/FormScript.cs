using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class FormScript : MonoBehaviour
{
    private AudioSource audioSource;
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public abstract void OnEnable();

    public void PlayAudio(string soundName)
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        EventDispatcher.Raise<PlaySound>(new PlaySound() {
            soundName = soundName,
            source = audioSource
        });
    }
}
