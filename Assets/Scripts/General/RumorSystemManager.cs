using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RumorSystemManager : MonoBehaviour
{
    #region Inspector Variables
    // -------------------------------------------------------------------------
    //  Inspector
    // -------------------------------------------------------------------------
    [Header("Menu Elements")] [SerializeField]
    [Tooltip("Reference to the side menu folder in pause")]
    private GameObject sideMenuFolder;
    
    [Tooltip("Reference to the menu character picture")]
    [SerializeField] private Image menuCharacterImage;
    [Tooltip("Reference to the menu character Title")]
    [SerializeField] private TextMeshProUGUI menuCharacterTitle;
    
    [Tooltip("Reference to the menu character bio text")]
    [SerializeField] private TextMeshProUGUI menuCharacterBioText;
    [Tooltip("Reference to the menu character tip Image")]
    [SerializeField] private Image menuCharacterBioImage;
    
    [Tooltip("Reference to the menu character tip text")]
    [SerializeField] private TextMeshProUGUI menuCharacterTipText;
    [Tooltip("Reference to the menu character tip Image")]
    [SerializeField] private Image menuCharacterTipImage;
    
    [Tooltip("Reference to the menu character rumor text")]
    [SerializeField] private TextMeshProUGUI menuCharacterRumorText;
    [Tooltip("Reference to the menu character rumor Image")]
    [SerializeField] private Image menuCharacterRumorImage;

    [Header("Level Rumors")]
    [SerializeField] private LevelRumorDatabase rumorDatabase;
    
    #endregion

    private bool isSubscribed;
    
    #region Singleton
    // -------------------------------------------------------------------------
    //  Singleton
    // -------------------------------------------------------------------------
    public static RumorSystemManager Instance { get; private set; }
    
    #endregion
    
    #region Unity Lifecycle
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
            return;
        }
        
        BindUIReferences();
    }
    
    private void OnEnable()
    {
        EventDispatcher.AddListener<NewSceneLoaded>(OnNewSceneLoaded);
        SubscribeToLevelManager();
    }

    private void OnDisable()
    {
        EventDispatcher.RemoveListener<NewSceneLoaded>(OnNewSceneLoaded);
        UnsubscribeFromLevelManager();
    }
    #endregion

    #region Event Handlers
    // -------------------------------------------------------------------------
    //  Event handlers
    // -------------------------------------------------------------------------
    
    private void OnLevelChange(LevelData level)
    {
        if (rumorDatabase is null) return;
        
        Debug.Log($"[RumorSystem] Level has changed and pulling rumors for level: '{level.levelId}'.", this);

        if (rumorDatabase.TryGetRumor(level.levelId, out var match))
        {
            ApplyRumorToUI(match);
        }
        else
        {
            Debug.LogWarning($"[RumorSystem] No rumor configured for '{level.levelId}'.", this);
        }
    }
    
    private void OnNewSceneLoaded(NewSceneLoaded scene)
    {
        // HACK: This if else should never be reached
        // Only clear UI when entering a non-level scene (menu, cutscene, etc.)
        if (LevelManager.Instance is null || !LevelManager.Instance.IsLevelScene(scene.sceneName, out LevelData level))
        {
            ClearUI();
        }
        else if (!isSubscribed)
        {
            OnLevelChange(level);
            SubscribeToLevelManager();
        }
    }
    
    #endregion
    
    #region Rumor Construction
    
    /// <summary>
    /// Binds UI references to required members.
    /// </summary>
    private void BindUIReferences()
    {
        // If references aren't passed try to find elements
        if (menuCharacterImage == null)
            menuCharacterImage = sideMenuFolder?.transform.Find("CharacterPicture")?.GetComponent<Image>();
        if (menuCharacterTitle == null)
            menuCharacterTitle = sideMenuFolder?.transform.Find("CharacterTitle")?.GetComponent<TextMeshProUGUI>();
        
        if (menuCharacterBioText == null)
            menuCharacterBioText = sideMenuFolder?.transform.Find("CharacterBio")?.GetComponent<TextMeshProUGUI>();
        if (menuCharacterBioImage == null)
            menuCharacterBioImage = sideMenuFolder?.transform.Find("CharacterBioImage")?.GetComponent<Image>();
        
        if (menuCharacterTipText == null)
            menuCharacterTipText = sideMenuFolder?.transform.Find("CharacterTip")?.GetComponent<TextMeshProUGUI>();
        if (menuCharacterTipImage == null)
            menuCharacterTipImage = sideMenuFolder?.transform.Find("CharacterTipImage")?.GetComponent<Image>();
        
        if (menuCharacterRumorText == null)
            menuCharacterRumorText = sideMenuFolder?.transform.Find("CharacterRumor")?.GetComponent<TextMeshProUGUI>();
        if (menuCharacterRumorImage == null)
            menuCharacterRumorImage = sideMenuFolder?.transform.Find("CharacterRumorImage")?.GetComponent<Image>();

        if (sideMenuFolder != null &&
            (menuCharacterImage == null || menuCharacterTitle == null 
             || menuCharacterBioText == null || menuCharacterBioImage == null 
             ||menuCharacterTipText == null || menuCharacterTipImage == null
             || menuCharacterRumorText == null || menuCharacterRumorImage == null))
        {
            Debug.LogError("[RumorSystem] Failed to auto-bind one or more UI references. Please assign them in the inspector.");
        }
    }
    
    #endregion

    #region UI Manipulation
    // -------------------------------------------------------------------------
    //  UI Manipulation
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// With passed rumor applies properties to menu element references.
    /// </summary>
    /// <param name="rumor"></param>
    private void ApplyRumorToUI(LevelRumorDatabase.LevelRumor rumor)
    {
        Debug.Log($"[RumorSystem] Filling in UI for Rumor level: '{rumor.LevelId}'", this);
        SetSprite(menuCharacterImage, rumor.CharacterPicture);
        ApplyTextProperties(menuCharacterTitle, rumor.CharacterTitle);
    
        ApplyTextProperties(menuCharacterBioText, rumor.CharacterBioText);
        SetSprite(menuCharacterBioImage, rumor.CharacterBioImage);
    
        ApplyTextProperties(menuCharacterTipText, rumor.CharacterTipText);
        SetSprite(menuCharacterTipImage, rumor.CharacterTipImage);
    
        ApplyTextProperties(menuCharacterRumorText, rumor.CharacterRumorText);
        SetSprite(menuCharacterRumorImage, rumor.CharacterRumorImage);
    }
    
    /// <summary>
    /// Sets text field in TMP text field. Typically use for setting to null/empty.
    /// </summary>
    /// <param name="textField"></param>
    /// <param name="value"></param>
    private void SetText(TextMeshProUGUI textField, string value)
    {
        if (textField != null)
            textField.text = value ?? string.Empty;
    }

    /// <summary>
    /// Copies source sprite into target Image element.
    /// </summary>
    /// <param name="imageField"></param>
    /// <param name="sprite"></param>
    private void SetSprite(Image imageField, Sprite sprite)
    {
        if (imageField != null)
            imageField.sprite = sprite;
    }

    /// <summary>
    /// Copies TextMeshPro properties from source to target element.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="source"></param>
    private void ApplyTextProperties(TextMeshProUGUI target, LevelRumorDatabase.RumorTextData source)
    {
        if (target == null || source == null) return;
    
        target.text = source.Text;
        target.font = source.Font;
        target.fontStyle = source.FontStyle;
        target.fontSize = source.FontSize;
        target.alignment = source.Alignment;
        target.characterSpacing = source.CharacterSpacing;
        target.wordSpacing = source.WordSpacing;
        target.lineSpacing = source.LineSpacing;
        target.paragraphSpacing = source.ParagraphSpacing;
    }
    
    /// <summary>
    /// Clears all Rumor UI areas.
    /// </summary>
    
    private void ClearUI()
    {
        SetSprite(menuCharacterImage, null);
        SetText(menuCharacterTitle, null);
        
        SetText(menuCharacterBioText, null);
        SetSprite(menuCharacterBioImage, null);
        
        SetText(menuCharacterTipText, null);
        SetSprite(menuCharacterTipImage, null);
        
        SetText(menuCharacterRumorText, null);
        SetSprite(menuCharacterRumorImage, null);
    }

    /// <summary>
    /// Function to show a specific Rumor in the UI section using the passed scene name.
    /// </summary>
    /// <param name="sceneName"></param>
    public void ShowRumor(LevelId levelId)
    {
        if (rumorDatabase != null && rumorDatabase.TryGetRumor(levelId, out var match))
        {
            ApplyRumorToUI(match);
        }
        else
        {
            Debug.LogWarning($"[RumorSystem] ShowRumor called with unknown level '{levelId}'.");
        }
    }
    
    #endregion
    
    #region Event Subscriptions

    private void SubscribeToLevelManager()
    {
        if (LevelManager.Instance != null && !isSubscribed)
        {
            LevelManager.Instance.OnLevelChanged += OnLevelChange;
            isSubscribed = true;
        }
        else
        {
            isSubscribed = false;
        }
    }

    private void UnsubscribeFromLevelManager()
    {
        if (LevelManager.Instance is not null && isSubscribed)
        {
            LevelManager.Instance.OnLevelChanged -= OnLevelChange;
        }

        isSubscribed = false;
    }
    
    #endregion
}
