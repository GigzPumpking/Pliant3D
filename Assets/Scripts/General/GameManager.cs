using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : KeyActionReceiver<GameManager>
{
    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }
    [SerializeField] private Transform player;
    private TransformationWheel transformWheel;

    [SerializeField] private AudioData mainTheme;

    // Static key mapping shared across all GameManager instances.
    public static Dictionary<string, Action<GameManager, InputAction.CallbackContext>> staticKeyMapping =
        new Dictionary<string, Action<GameManager, InputAction.CallbackContext>>()
        {
            { "Pause", (manager, ctx) => manager.Pause(ctx) },
            { "Reset", (manager, ctx) => manager.Reset(ctx) }
        };

    protected override Dictionary<string, Action<GameManager, InputAction.CallbackContext>> KeyMapping => staticKeyMapping;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(this.gameObject);
            return;
        }
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        AudioManager.Instance?.PlayMusic(mainTheme);
    }

    public void SetPlayer(Transform player)
    {
        this.player = player;
    }

    public Transform GetPlayer()
    {
        if (player == null)
        {
            player = Player.Instance?.transform;
        }

        return player;
    }

    public void Pause()
    {
        UIManager.Instance?.Pause();
    }

    public void Pause(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Pause();
        }
    }

    public void Reset()
    {
        // Restart the game
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        Player.Instance?.SetTransformation(Transformation.TERRY);
        // set Player velocity to 0
        Player.Instance?.SetVelocity(Vector3.zero);
        if (transformWheel == null) transformWheel = Player.Instance?.GetComponentInChildren<TransformationWheel>();
        transformWheel?.ResetProgress();
    }

    public void Reset(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Reset();
        }
    }

    public void Quit()
    {
        Application.Quit();
    }

}
