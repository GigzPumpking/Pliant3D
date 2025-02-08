using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    public static AudioManager Instance { get { return instance; } }
    public Sound[] sounds;
    public Sound[] music;
    public AudioSource[] sources;

    private void Awake()
    {
        if (instance != null && instance != this)
            Destroy(this.gameObject);
        else
            instance = this;

        DontDestroyOnLoad(this);
        EventDispatcher.AddListener<PlaySound>(PlaySound);
        EventDispatcher.AddListener<PlayMusic>(PlayMusic);
        EventDispatcher.AddListener<StopMusic>(StopMusic);
    }

    private void OnDestroy()
    {
        EventDispatcher.RemoveListener<PlaySound>(PlaySound);
        EventDispatcher.RemoveListener<PlayMusic>(PlayMusic);
        EventDispatcher.RemoveListener<StopMusic>(StopMusic);
    }

    public void PlaySound(PlaySound evtData)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == evtData.soundName);

        if (s == null) {
            Debug.LogWarning("Sound: " + evtData.soundName + " not found!");
            return;
        }
        else {
            AudioSource source = evtData.source;
            if (source == null) {
                source = System.Array.Find(sources, s => !s.isPlaying);
                if (source == null) {
                    // If all sources are playing, find the first one in the list
                    if (sources[0] == null) {
                        Debug.LogWarning("No available audio sources!");
                        return;
                    } 
                    source = sources[0];
                }
            }
            source.clip = s.clip;
            source.Play();
        }
    }

    public void PlayMusic(PlayMusic evtData)
    {
        Sound s = System.Array.Find(music, sound => sound.name == evtData.musicName);

        if (s == null) {
            Debug.LogWarning("Music: " + evtData.musicName + " not found!");
            return;
        }
        else {
            // From a list of sources, find the first one that is not playing
            AudioSource source = System.Array.Find(sources, s => !s.isPlaying);
            if (source == null) {
                // If all sources are playing, find the first one in the list
                if (sources[0] == null) {
                    Debug.LogWarning("No available audio sources!");
                    return;
                } 
                source = sources[0];
            }
            source.clip = s.clip;
            source.Play();
        }
    }

    public void StopMusic(StopMusic evtData)
    {
        Sound s = System.Array.Find(music, sound => sound.name == evtData.musicName);

        if (s == null) {
            Debug.LogWarning("Music: " + evtData.musicName + " not found!");
            return;
        }
        else {
            AudioSource source = System.Array.Find(sources, s => s.clip == s.clip);
            if (source == null) {
                Debug.LogWarning("No audio source is playing that clip!");
                return;
            } else {
                source.Stop();
            }
        }
    }

}
