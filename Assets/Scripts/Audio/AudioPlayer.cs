using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [SerializeField] private List<AudioData> audioClips = new List<AudioData>();

    public void PlayOneShot(string clipName, Transform sourceTransform = null)
    {
        AudioData clip = audioClips.Find(a => a.name == clipName);
        if (clip == null)
        {
            Debug.LogWarning("Audio clip not found!");
            return;
        }

        AudioManager.Instance?.PlayOneShot(clip, sourceTransform);
    }

    public void PlaySound(string clipName, Transform sourceTransform = null)
    {
        AudioData clip = audioClips.Find(a => a.name == clipName);
        if (clip == null)
        {
            Debug.LogWarning("Audio clip not found!");
            return;
        }

        AudioManager.Instance?.PlaySound(clip, sourceTransform);
    }

    public void PlayMusic(string clipName)
    {
        AudioData clip = audioClips.Find(a => a.name == clipName);
        if (clip == null)
        {
            Debug.LogWarning("Audio clip not found!");
            return;
        }

        AudioManager.Instance?.PlayMusic(clip);
    }

    public void StopSound(string clipName)
    {
        AudioData clip = audioClips.Find(a => a.name == clipName);
        if (clip == null)
        {
            Debug.LogWarning("Audio clip not found!");
            return;
        }

        AudioManager.Instance?.StopSound(clip);
    }

    public void IsMusicPlaying(string clipName)
    {
        AudioData clip = audioClips.Find(a => a.name == clipName);
        if (clip == null)
        {
            Debug.LogWarning("Audio clip not found!");
            return;
        }

        AudioManager.Instance?.IsMusicPlaying(clip);
    }

    public void IsSoundPlaying(string clipName)
    {
        AudioData clip = audioClips.Find(a => a.name == clipName);
        if (clip == null)
        {
            Debug.LogWarning("Audio clip not found!");
            return;
        }

        AudioManager.Instance?.IsSoundPlaying(clip);
    }
}
