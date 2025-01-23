using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class FormScript : MonoBehaviour
{
    private AudioSource audioSource;
    protected Transform player;
    protected Player playerScript;
    protected Rigidbody rb;
    protected Animator animator;
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
    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        player = transform.parent;

        playerScript = player.GetComponent<Player>();

        rb = player.GetComponent<Rigidbody>();

        animator = GetComponentInChildren<Animator>();
    }

    public virtual void OnEnable() {
        speed = baseSpeed;
    }

    public abstract void Ability1(InputAction.CallbackContext context);

    public abstract void Ability2(InputAction.CallbackContext context);

    public void PlayAudio(string soundName)
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        EventDispatcher.Raise<PlaySound>(new PlaySound() {
            soundName = soundName,
            source = audioSource
        });
    }

    public float GetSpeed()
    {
        return speed;
    }
}
