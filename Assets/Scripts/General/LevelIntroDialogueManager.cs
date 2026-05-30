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

    [Header("Dialogue Portrait")]
    [Tooltip("Sprite to display in the dialogue box portrait image when this NPC speaks.")]
    [SerializeField] private Sprite npcPortrait;

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

    // Cached reference to Terry's Animator so we can check when the Tired animation finishes.
    private Animator _terryAnimator;

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
        if (_activeFirstEntry == null || e.someEntry != _activeFirstEntry)
            return;

        _activeFirstEntry = null;

        // If the Tired animation is still playing when the dialogue ends, keep movement
        // locked until the animation finishes (Dialogue.cs re-enables movement after this
        // callback, so we wait one frame and then re-block).
        if (_terryAnimator != null &&
            _terryAnimator.GetCurrentAnimatorStateInfo(0).IsName("FrontLeft_Tired_Terry"))
        {
            StartCoroutine(WaitForTiredAnimationToFinish());
        }
    }

    private IEnumerator WaitForTiredAnimationToFinish()
    {
        // Yield one frame so Dialogue.cs's TogglePlayerMovement { isEnabled = true } fires first.
        yield return null;

        // Re-block movement while the Tired animation is still running.
        EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = false });

        yield return new WaitUntil(() =>
            _terryAnimator == null ||
            !_terryAnimator.GetCurrentAnimatorStateInfo(0).IsName("FrontLeft_Tired_Terry"));

        EventDispatcher.Raise<TogglePlayerMovement>(new TogglePlayerMovement() { isEnabled = true });
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

        // Fire the "Tired" trigger on Terry's animator now that the transformation has
        // been reset to TERRY by the NewSceneLoaded handler.
        _terryAnimator = Player.Instance.transform.Find("Terry")?.GetComponentInChildren<Animator>();
        _terryAnimator?.SetTrigger("Tired");

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
        dialogue.SetPortrait(npcPortrait);
    }
}
