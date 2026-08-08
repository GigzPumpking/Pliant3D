using UnityEngine;
using UnityEngine.InputSystem;
using System;

public abstract class FormScript : MonoBehaviour
{
    protected Transform player;

    protected Player playerScript;

    protected Rigidbody rb;

    protected Animator animator;

    [SerializeField] protected AudioData initialSound;
    [SerializeField] protected AudioData ability1Sound;
    [SerializeField] protected AudioData ability2Sound;
    [SerializeField] protected AudioData walkSound;

    [SerializeField] protected abstract float baseSpeed { get; set; }

    private float _speed; // Backing field for the speed property
    private bool isWalkSoundPlaying = false;

    protected virtual float speed
    {
        get => _speed;
        set
        {
            _speed = value;
            if (playerScript != null)
            {
                playerScript.SetSpeed(_speed); // Automatically update player speed
            }
            else
            {
                Debug.LogWarning("playerScript is null! Unable to set speed.");
            }
        }
        
    }

    protected virtual void PlayAbilitySound(AudioData data)
    {
        if(data != null) AudioManager.Instance?.PlayOneShot(data);
    }
    
    protected virtual void PlayAbilitySoundLooping(AudioData data)
    {
        if (data == null) return;
        if (AudioManager.Instance.IsSoundPlaying(data)) return;
        if (!data.loop) data.loop = true;
        AudioManager.Instance?.PlaySound(data);
    }

    protected virtual void StopAbilitySound(AudioData data)
    {
        if (data != null && AudioManager.Instance?.IsSoundPlaying(data) == true) 
            AudioManager.Instance?.StopSound(data);
    }

    public virtual void PlayWalkSound()
    {
        if (walkSound != null && !isWalkSoundPlaying)
        {
            if (!walkSound.loop) walkSound.loop = true;
            AudioManager.Instance?.PlaySound(walkSound);
            isWalkSoundPlaying = true;
        }
    }

    public virtual void EndWalkSound()
    {
        if (walkSound != null && isWalkSoundPlaying)
        {
            AudioManager.Instance?.StopSound(walkSound);
            isWalkSoundPlaying = false;
        }
    }
    
    public virtual void Awake()
    {
        player = transform.parent;
        
        playerScript = player.GetComponent<Player>();

        rb = player.GetComponent<Rigidbody>();

        animator = GetComponentInChildren<Animator>();
    }

    public virtual void OnEnable() {
        speed = baseSpeed;
        AudioManager.Instance?.PlayOneShot(initialSound);
    }

    public abstract void Ability1(InputAction.CallbackContext context);


    public abstract void Ability2(InputAction.CallbackContext context);


    public virtual void Ability3(InputAction.CallbackContext context)
    {
        // Optional ability, can be overridden by subclasses
    }

    public virtual void Unstick(InputAction.CallbackContext context)
    {
        // Optional unstick minigame input, can be overridden by subclasses
    }

    /// <summary>
    /// When true, Player will not update facing direction, sprite flip, or
    /// animation move floats. Override in subclasses that need to lock facing
    /// (e.g. Bulldozer while pushing).
    /// </summary>
    public virtual bool IsDirectionLocked => false;

    public float GetSpeed()
    {
        return speed;
    }
}
