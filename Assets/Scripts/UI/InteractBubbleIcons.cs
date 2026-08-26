using UnityEngine;

// Central source of interact-bubble sprites so every script that shows one uses the same set
// instead of each having its own Inspector-assigned sprites. Configure them on the
// InteractBubbleIcons asset in Assets/Resources.
public class InteractBubbleIcons : ScriptableObject
{
    private const string ResourcePath = "InteractBubbleIcons";

    [SerializeField] private Sprite keyboardSprite;
    [SerializeField] private Sprite controllerSprite;
    [SerializeField] private Sprite holdKeyboardSprite;
    [SerializeField] private Sprite holdControllerSprite;

    private static InteractBubbleIcons _instance;
    private static InteractBubbleIcons Instance => _instance != null ? _instance : (_instance = Resources.Load<InteractBubbleIcons>(ResourcePath));

    public static Sprite Keyboard => Instance != null ? Instance.keyboardSprite : null;
    public static Sprite Controller => Instance != null ? Instance.controllerSprite : null;
    public static Sprite HoldKeyboard => Instance != null ? Instance.holdKeyboardSprite : null;
    public static Sprite HoldController => Instance != null ? Instance.holdControllerSprite : null;
}
