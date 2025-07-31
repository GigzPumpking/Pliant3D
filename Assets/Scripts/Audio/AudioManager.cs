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

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource; // Dedicated music AudioSource

    [Header("Volume Controls")]
    [SerializeField, Range(0f, 1f)]
    private float globalVolume = 1f;
    [SerializeField, Range(0f, 1f)]
    private float overallMusicVolume = 1f;
    [SerializeField, Range(0f, 1f)]
    private float overallSFXVolume = 1f;

    [Header("SFX Pitch Randomization")]
    public bool randomizePitch = true;
    [SerializeField] float lowestPitch = .95f;
    [SerializeField] float highestPitch = 1.05f;

    // Internal State
    private List<AudioSource> activeSources = new List<AudioSource>(); // Tracks active looping sounds
    private AudioData currentMusicData;

    // Mute state variables
    private bool isGlobalMuted = false;
    private bool isMusicMuted = false;
    private bool isSfxMuted = false;
    private float savedGlobalVolume;
    private float savedMusicVolume;
    private float savedSfxVolume;


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
            return;
        }

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
        if (UIManager.Instance?.isPaused == true)
        {
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                AudioSource src = activeSources[i];
                src.Stop();
                AudioPool.Instance.ReturnAudioSource(src);
                activeSources.RemoveAt(i);
            }
        }
    }

    // --- Sound Playback ---

    public void PlayOneShot(AudioData data, Transform parent)
    {
        if (data == null || data.clip == null) return;
        AudioSource source = AudioPool.Instance.GetAudioSource(parent);
        if (source != null)
        {
            source.pitch = randomizePitch ? UnityEngine.Random.Range(lowestPitch, highestPitch) : 1f;
            source.spatialBlend = 1.0f;
            source.volume = data.volume * overallSFXVolume * globalVolume;
            source.PlayOneShot(data.clip);
            StartCoroutine(ReturnAfterPlay(source, data.clip.length, parent));
        }
    }

    public void PlayOneShot(AudioData data)
    {
        if (data == null || data.clip == null) return;
        AudioSource source = AudioPool.Instance.GetAudioSource(null);
        if (source != null)
        {
            source.pitch = randomizePitch ? UnityEngine.Random.Range(lowestPitch, highestPitch) : 1f;
            source.spatialBlend = 0.0f;
            source.volume = data.volume * overallSFXVolume * globalVolume;
            source.PlayOneShot(data.clip);
            StartCoroutine(ReturnAfterPlay(source, data.clip.length, null));
        }
    }

    public AudioSource PlaySound(AudioData data, Transform parent)
    {
        if (data == null || data.clip == null) return null;
        AudioSource source = AudioPool.Instance.GetAudioSource(parent);
        if (source != null)
        {
            source.clip = data.clip;
            source.pitch = randomizePitch ? UnityEngine.Random.Range(lowestPitch, highestPitch) : 1f;
            source.volume = data.volume * overallSFXVolume * globalVolume;
            source.loop = data.loop;
            source.spatialBlend = 1.0f;
            source.Play();
            if (data.loop) activeSources.Add(source);
        }
        return source;
    }

    public AudioSource PlaySound(AudioData data)
    {
        if (data == null || data.clip == null) return null;
        AudioSource source = AudioPool.Instance.GetAudioSource(null);
        if (source != null)
        {
            source.clip = data.clip;
            source.pitch = randomizePitch ? UnityEngine.Random.Range(lowestPitch, highestPitch) : 1f;
            source.volume = data.volume * overallSFXVolume * globalVolume;
            source.loop = data.loop;
            source.spatialBlend = 0.0f;
            source.Play();
            if (data.loop) activeSources.Add(source);
        }
        return source;
    }

    public void StopSound(AudioData data)
    {
        if (data == null || data.clip == null) return;
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

    // --- Music Playback ---

    public void PlayMusic(AudioData data)
    {
        if (data == null || data.clip == null) return;
        if (musicSource != null)
        {
            currentMusicData = data;
            musicSource.clip = data.clip;
            musicSource.loop = data.loop;
            musicSource.spatialBlend = 0.0f;
            UpdateCurrentMusicVolume();
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

    // --- State Checks ---

    public bool IsSoundPlaying(AudioData data)
    {
        if (data == null || data.clip == null) return false;
        foreach (AudioSource source in activeSources)
        {
            if (source.clip == data.clip && source.isPlaying) return true;
        }
        return false;
    }

    public bool IsMusicPlaying() => musicSource.isPlaying;

    // --- Volume & Mute Controls ---

    private void UpdateCurrentMusicVolume()
    {
        if (musicSource != null && currentMusicData != null && musicSource.isPlaying)
        {
            musicSource.volume = currentMusicData.volume * overallMusicVolume * globalVolume;
        }
    }

    public void SetGlobalVolume(float volume)
    {
        globalVolume = Mathf.Clamp01(volume);
        isGlobalMuted = false;
        UpdateCurrentMusicVolume();
    }

    public void SetMusicVolume(float volume)
    {
        overallMusicVolume = Mathf.Clamp01(volume);
        isMusicMuted = false;
        UpdateCurrentMusicVolume();
    }

    public void SetSFXVolume(float volume)
    {
        overallSFXVolume = Mathf.Clamp01(volume);
        isSfxMuted = false;
    }

    public void ToggleGlobalMute()
    {
        isGlobalMuted = !isGlobalMuted;
        if (isGlobalMuted)
        {
            savedGlobalVolume = globalVolume;
            globalVolume = 0f;
        }
        else
        {
            globalVolume = savedGlobalVolume;
        }
        UpdateCurrentMusicVolume();
    }

    public void ToggleMusicMute()
    {
        isMusicMuted = !isMusicMuted;
        if (isMusicMuted)
        {
            savedMusicVolume = overallMusicVolume;
            overallMusicVolume = 0f;
        }
        else
        {
            overallMusicVolume = savedMusicVolume;
        }
        UpdateCurrentMusicVolume();
    }

    public void ToggleSfxMute()
    {
        isSfxMuted = !isSfxMuted;
        if (isSfxMuted)
        {
            savedSfxVolume = overallSFXVolume;
            overallSFXVolume = 0f;
        }
        else
        {
            overallSFXVolume = savedSfxVolume;
        }
    }

    // --- Coroutines & Event Handlers ---

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
        PlayOneShot(data, parent);
    }
}