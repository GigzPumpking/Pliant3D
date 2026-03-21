using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton manager that automatically triggers introductory dialogue at the start of
/// specified levels. Player movement is blocked until the dialogue completes.
///
/// Place this on a persistent root GameObject (or let it self-persist via DontDestroyOnLoad).
/// In the Inspector, populate "Level Dialogues" with one entry per level that needs an intro.
/// </summary>
public class LevelIntroDialogueManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    //  Data
    // -------------------------------------------------------------------------

    [System.Serializable]
    public class LevelDialogue
    {
        [Tooltip("Exact scene name (as it appears in Build Settings) that should trigger this intro dialogue.")]
        public string sceneName;

        [Tooltip("Dialogue entries displayed when this level starts. Supports all standard formatting and device-specific variants.")]
        public DialogueEntry[] dialogueEntries;
    }

    // -------------------------------------------------------------------------
    //  Inspector
    // -------------------------------------------------------------------------

    [Header("Level Intro Dialogues")]
    [Tooltip("Each entry maps a scene name to the intro dialogue that plays when that level loads.")]
    [SerializeField] private List<LevelDialogue> levelDialogues = new List<LevelDialogue>();

    [Tooltip("When true, each scene's intro plays only once per session even if the scene is reloaded (e.g. after a game-over reset). Disable to replay every time the scene loads.")]
    [SerializeField] private bool triggerOnlyOnce = true;

    // -------------------------------------------------------------------------
    //  Singleton
    // -------------------------------------------------------------------------

    public static LevelIntroDialogueManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    //  State
    // -------------------------------------------------------------------------

    // Scene names whose intro dialogue has already been shown this session.
    private readonly HashSet<string> _triggeredScenes = new HashSet<string>();

    // defaultText of the first entry of the currently playing intro dialogue,
    // used to match the EndDialogue event raised by Dialogue.cs.
    private string _activeFirstEntry = null;

    // -------------------------------------------------------------------------
    //  Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        EventDispatcher.AddListener<NewSceneLoaded>(OnNewSceneLoaded);
        EventDispatcher.AddListener<EndDialogue>(OnEndDialogue);
    }

    private void OnDisable()
    {
        EventDispatcher.RemoveListener<NewSceneLoaded>(OnNewSceneLoaded);
        EventDispatcher.RemoveListener<EndDialogue>(OnEndDialogue);
    }

    // -------------------------------------------------------------------------
    //  Event handlers
    // -------------------------------------------------------------------------

    private void OnNewSceneLoaded(NewSceneLoaded e)
    {
        if (triggerOnlyOnce && _triggeredScenes.Contains(e.sceneName))
            return;

        LevelDialogue match = levelDialogues.Find(ld => ld.sceneName == e.sceneName);
        if (match == null || match.dialogueEntries == null || match.dialogueEntries.Length == 0)
            return;

        StartCoroutine(TriggerIntroDialogue(match, e.sceneName));
    }

    private void OnEndDialogue(EndDialogue e)
    {
        // Dialogue.cs already raises TogglePlayerMovement { isEnabled = true } on the last
        // line, so movement is re-enabled automatically. We only need to clear our state.
        if (_activeFirstEntry == null || e.someEntry != _activeFirstEntry)
            return;

        _activeFirstEntry = null;
    }

    // -------------------------------------------------------------------------
    //  Coroutine
    // -------------------------------------------------------------------------

    private IEnumerator TriggerIntroDialogue(LevelDialogue levelDialogue, string loadedSceneName)
    {
        // Wait until the persistent singletons are ready (they should be, but guard anyway).
        yield return new WaitUntil(() => UIManager.Instance != null && Player.Instance != null);

        // One extra frame so every scene object's Start() has run before we block input.
        yield return null;

        Dialogue dialogue = UIManager.Instance.returnDialogue();
        if (dialogue == null || dialogue.IsActive())
            yield break;

        // Mark triggered before starting in case of re-entrance.
        if (triggerOnlyOnce)
            _triggeredScenes.Add(loadedSceneName);

        // Block player movement.
        EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = false });

        // Set up and display the dialogue.
        dialogue.SetDialogueEntries(levelDialogue.dialogueEntries);
        _activeFirstEntry = levelDialogue.dialogueEntries[0].defaultText;
        dialogue.Appear();
    }
}
