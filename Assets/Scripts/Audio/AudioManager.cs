using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AudioData
{
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(.1f, 3f)]
    public float pitch = 1f;
    public bool loop;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public static event Action<AudioData, Transform> OnPlayPooledSFX;

    [SerializeField] private AudioSource musicSource; // Dedicated music AudioSource
    [SerializeField, Range(0f, 1f)]
    private float overallMusicVolume = 1f; 

    [SerializeField, Range(0f, 1f)]
    private float overallSFXVolume = 1f;  

    private List<AudioSource> activeSources = new List<AudioSource>(); // Tracks active looping sounds

    // Field to store the currently playing music AudioData.
    private AudioData currentMusicData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            OnPlayPooledSFX += HandlePlayPooledSFX;
        }
        else
        {
            Destroy(gameObject);
        }

        musicSource = GetComponent<AudioSource>();

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnDestroy()
    {
        OnPlayPooledSFX -= HandlePlayPooledSFX;
    }

    private void Update()
    {
        // If the pause menu is active, stop any currently looping sounds.
        if (UIManager.Instance?.isPaused == true)
        {
            // Iterate backwards to safely remove items.
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                AudioSource src = activeSources[i];
                src.Stop();
                AudioPool.Instance.ReturnAudioSource(src);
                activeSources.RemoveAt(i);
            }
        }
    }

    public void PlayOneShot(AudioData data, Transform parent)
    {
        if (data == null || data.clip == null)
        {
            Debug.LogWarning("AudioData or AudioClip is null for Transform: " + parent);
            return;
        }

        AudioSource source = AudioPool.Instance.GetAudioSource(parent);
        if (source != null)
        {
            source.spatialBlend = 1.0f;
            source.volume = data.volume * overallSFXVolume;
            source.PlayOneShot(data.clip);

            StartCoroutine(ReturnAfterPlay(source, data.clip.length, parent));
        }
    }

    public void PlayOneShot(AudioData data)
    {
        if (data == null || data.clip == null)
        {
            Debug.LogWarning("AudioData or AudioClip is null for global sound.");
            return;
        }

        AudioSource source = AudioPool.Instance.GetAudioSource(null);
        if (source != null)
        {
            source.spatialBlend = 0.0f;
            source.volume = data.volume * overallSFXVolume;
            source.PlayOneShot(data.clip);

            StartCoroutine(ReturnAfterPlay(source, data.clip.length, null));
        }
    }

    public AudioSource PlaySound(AudioData data, Transform parent)
    {
        if (data == null || data.clip == null)
        {
            Debug.LogWarning("AudioData or AudioClip is null for Transform: " + parent);
            return null;
        }

        AudioSource source = AudioPool.Instance.GetAudioSource(parent);
        if (source != null)
        {
            source.clip = data.clip;
            source.volume = data.volume * overallSFXVolume;
            source.loop = data.loop;
            source.spatialBlend = 1.0f;
            source.Play();

            if (data.loop)
            {
                activeSources.Add(source);
            }
        }
        return source;
    }

    public AudioSource PlaySound(AudioData data)
    {
        if (data == null || data.clip == null)
        {
            Debug.LogWarning("AudioData or AudioClip is null for global sound.");
            return null;
        }

        AudioSource source = AudioPool.Instance.GetAudioSource(null);
        if (source != null)
        {
            source.clip = data.clip;
            source.volume = data.volume * overallSFXVolume;
            source.loop = data.loop;
            source.spatialBlend = 0.0f;
            source.Play();

            if (data.loop)
            {
                activeSources.Add(source);
            }
        }
        return source;
    }

    public bool IsSoundPlaying(AudioData data)
    {
        if (data == null || data.clip == null)
        {
            return false;
        }

        foreach (AudioSource source in activeSources)
        {
            if (source.clip == data.clip && source.isPlaying)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsMusicPlaying(AudioData data)
    {
        if (data == null || data.clip == null)
        {
            return false;
        }

        return musicSource.clip == data.clip && musicSource.isPlaying;
    }

    public bool IsMusicPlaying()
    {
        return musicSource.isPlaying;
    }

    public void StopSound(AudioData data)
    {
        if (data == null || data.clip == null)
        {
            return;
        }

        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            if (activeSources[i].clip == data.clip)
            {
                activeSources[i].Stop();
                AudioPool.Instance.ReturnAudioSource(activeSources[i]);
                activeSources.RemoveAt(i);
            }
        }
    }

    public void PlayMusic(AudioData data)
    {
        if (data == null || data.clip == null)
        {
            Debug.LogWarning("AudioData or AudioClip is null for music.");
            return;
        }

        if (musicSource != null)
        {
            currentMusicData = data; // Store the current music data.
            musicSource.clip = data.clip;
            musicSource.volume = data.volume * overallMusicVolume;
            musicSource.loop = data.loop;
            musicSource.spatialBlend = 0.0f;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    private IEnumerator ReturnAfterPlay(AudioSource source, float delay, Transform parent)
    {
        yield return new WaitForSeconds(delay);

        if (parent == null)
        {
            source.transform.SetParent(null);
        }

        AudioPool.Instance.ReturnAudioSource(source);
    }

    private void HandlePlayPooledSFX(AudioData data, Transform parent)
    {
        if (data == null || data.clip == null)
        {
            Debug.LogWarning("AudioData or AudioClip is null for Transform: " + parent);
            return;
        }

        PlayOneShot(data, parent);
    }

    // Public methods to adjust volume via UI sliders
    public void SetSFXVolume(float volume)
    {
        overallSFXVolume = volume;
    }

    public void SetMusicVolume(float volume)
    {
        overallMusicVolume = volume;
        // Update the current music volume using the stored AudioData.
        if (musicSource != null && currentMusicData != null)
        {
            musicSource.volume = currentMusicData.volume * overallMusicVolume;
        }
    }
}
