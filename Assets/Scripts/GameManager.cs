using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : KeyActionReceiver
{
    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }
    [SerializeField] private Transform player;
    private TransformationWheel transformWheel;

    [SerializeField] private AudioData mainTheme;

    private void InitializeActionMap()
    {
        actionMap = new Dictionary<string, System.Action<InputAction.CallbackContext>>()
        {
            { "Pause", ctx => { if (ctx.performed) Pause(); } },
            { "Reset", ctx => { if (ctx.performed) Reset(); } }
        };

        foreach (var action in actionMap.Keys)
        {
            EventDispatcher.Raise<DebugMessage>(new DebugMessage() { message = $"Adding keybind for {action} to action map" });
            InputManager.Instance?.AddKeyBind(this, action, "Gameplay");
        }
    }

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

        InitializeActionMap();
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

    public void Quit()
    {
        Application.Quit();
    }

}
