using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ModeOfCursor
{
    Default,
    Hand
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
    
    private Image gamepadCursorImage;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //TODO: Possibly uncomment if Cursor controller stays active between scenes (active during pause menu) will need to creat fuctionality to reattach serialized fields between scenes
            //DontDestroyOnLoad(gameObject);
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
}
