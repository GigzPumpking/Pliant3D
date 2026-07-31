using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ModeOfCursor
{
    Default,
    Hand
}

public enum ClickSoundType
{
    Single,
    Double
}

public class CursorController : MonoBehaviour
{
    
    public static CursorController Instance { get; private set; }
    
    [SerializeField] private Texture2D cursorTextureDefault;
    [SerializeField] private Texture2D cursorTextureHand;
    
    [SerializeField] private Sprite cursorSpriteDefault;
    [SerializeField] private Sprite cursorSpriteHand;
    
    [SerializeField] private Vector2 clickPosition = Vector2.zero;

    [SerializeField] private GameObject gamepadCursor;
    
    [SerializeField] private AudioData hoverSound;
    [SerializeField] private AudioData singleClickSound;
    [SerializeField] private AudioData doubleClickSound;
    
    private Image gamepadCursorImage;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            {
                Destroy(gameObject);
            }
        }
    }
    void Start()
    {
        Cursor.SetCursor(cursorTextureDefault, clickPosition, CursorMode.Auto);
        gamepadCursorImage = gamepadCursor.GetComponent<Image>();

    }

    public void SetToMode(ModeOfCursor modeOfCursor)
    {
        switch (modeOfCursor)
        {
            case ModeOfCursor.Default:
                Cursor.SetCursor(cursorTextureDefault, clickPosition, CursorMode.Auto);
                gamepadCursorImage.sprite = cursorSpriteDefault;
                break;
            case ModeOfCursor.Hand:
                Cursor.SetCursor(cursorTextureHand, clickPosition, CursorMode.Auto);
                gamepadCursorImage.sprite = cursorSpriteHand;
                break;
            default:
                Cursor.SetCursor(cursorTextureDefault, clickPosition, CursorMode.Auto);
                gamepadCursorImage.sprite = cursorSpriteDefault;
                break;
        }
    }

    public void PlayHoverSound()
    {
        AudioManager.Instance?.PlayOneShot(hoverSound);
    }

    public void PlayClickSound(ClickSoundType clickType)
    {
        switch (clickType)
        {
            case ClickSoundType.Single:
                AudioManager.Instance?.PlayOneShot(singleClickSound);
                break;
            case ClickSoundType.Double:
                AudioManager.Instance?.PlayOneShot(doubleClickSound);
                break;
            default:
                AudioManager.Instance?.PlayOneShot(singleClickSound);
                break;
        }
    }
}
