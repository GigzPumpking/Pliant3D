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

    [SerializeField] protected abstract float baseSpeed { get; set; }

    private float _speed; // Backing field for the speed property

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
