using UnityEngine;

/// <summary>
/// Plays a looping ambient sfx while this object is active. If an AnimTrigger is
/// assigned as the stop condition, the ambient sfx stops as soon as that trigger fires.
/// </summary>
public class AmbientSfxPlayer : MonoBehaviour
{
    [Tooltip("Looping ambient sound played while this object is active.")]
    [SerializeField] private AudioData ambientSound;

    [Tooltip("Optional. When assigned, the ambient sfx stops as soon as this AnimTrigger is triggered.")]
    [SerializeField] private AnimTrigger stopCondition;

    private AudioSource _ambientAudioSource;
    private bool _stopped = false;

    [Tooltip("Read-only. Reflects whether the ambient sfx is currently audible.")]
    [SerializeField] private bool isPlaying;

    private void OnEnable()
    {
        _stopped = stopCondition != null && stopCondition.IsTriggered;
        if (!_stopped) StartAmbientAudio();
    }

    private void OnDisable()
    {
        StopAmbientAudio();
    }

    private void Update()
    {
        if (_stopped) return;

        if (stopCondition != null && stopCondition.IsTriggered)
        {
            _stopped = true;
            StopAmbientAudio();
            return;
        }

        // AudioManager/AudioPool singletons may not be ready yet on the very first scene load, so keep retrying until it actually starts.
        if (_ambientAudioSource == null) StartAmbientAudio();

        isPlaying = _ambientAudioSource != null && _ambientAudioSource.isPlaying;
    }

    private void StartAmbientAudio()
    {
        if (ambientSound == null || ambientSound.clip == null) return;
        if (!ambientSound.loop) ambientSound.loop = true;
        _ambientAudioSource = AudioManager.Instance?.PlaySound(ambientSound, transform);
    }

    private void StopAmbientAudio()
    {
        if (_ambientAudioSource == null) return;
        AudioManager.Instance?.StopSound(ambientSound);
        _ambientAudioSource = null;
        isPlaying = false;
    }
}
