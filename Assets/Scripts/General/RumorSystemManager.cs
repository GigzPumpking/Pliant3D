using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RumorSystemManager : MonoBehaviour
{
    #region Data
    // -------------------------------------------------------------------------
    //  Data
    // -------------------------------------------------------------------------

    [System.Serializable]
    public class RumorTextData
    {
        [Tooltip("The text content to display.")] [TextArea(5, 10)]
        public string text = string.Empty;
        
        [Tooltip("Font asset to use for this text.")]
        public TMP_FontAsset font;
        
        [Tooltip("Font style to apply to the text")]
        public FontStyles fontStyle = FontStyles.Normal;
        
        [Tooltip("Font size for this text.")]
        public float fontSize = 36f;
        
        [Tooltip("Text alignment.")]
        public TextAlignmentOptions alignment = TextAlignmentOptions.Center;
    }
    
    [System.Serializable]
    public class LevelRumor
    {
        [Tooltip("Exact scene name (as it appears in Build Settings) that should trigger this rumor entry.")]
        public string sceneName;

        [Tooltip("Character Picture for Rumor")]
        public Sprite characterPicture;
        
        [Tooltip("Character Title for Rumor")]
        public RumorTextData characterTitle;

        [Tooltip("Character info text")]
        public RumorTextData characterBioText;
        
        [Tooltip("Character Bio Background Image for Rumor")]
        public Sprite characterBioImage;

        [Tooltip("Character Tip text")]
        public RumorTextData characterTipText;
        
        [Tooltip("Character Tip Background Image for Rumor")]
        public Sprite characterTipImage;

        [Tooltip("Character Rumor text")]
        public RumorTextData characterRumorText;
        
        [Tooltip("Character Rumor Background Image for Rumor")]
        public Sprite characterRumorImage;
    }
    #endregion
    
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

    [Header("Level Rumors")] [Tooltip("Each entry maps a scene name to the rumor that shows when that level loads.")]
    [SerializeField] private List<LevelRumor> levelRumors = new List<LevelRumor>();
    
    private Dictionary<string, LevelRumor> rumorMap;
    
    #endregion
    
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
        
        BuildRumorMap();
        BindUIReferences();
    }
    
    private void OnEnable()
    {
        EventDispatcher.AddListener<NewSceneLoaded>(OnNewSceneLoaded);
    }

    private void OnDisable()
    {
        EventDispatcher.RemoveListener<NewSceneLoaded>(OnNewSceneLoaded);
    }
    #endregion

    #region Event Handlers
    // -------------------------------------------------------------------------
    //  Event handlers
    // -------------------------------------------------------------------------
    
    private void OnNewSceneLoaded(NewSceneLoaded scene)
    {
       if (rumorMap == null) return;

       if (!rumorMap.TryGetValue(scene.sceneName, out LevelRumor match))
       {
           Debug.LogWarning($"[RumorSystem] No rumor configured for scene '{scene.sceneName}'.", this);
           //TODO: Uncomment this if wanting to blank out UI element if no scene is found. Currently want UI elements to carry over during levels
           //ClearUI();
           return;
       }
       
       ApplyRumorToUI(match);
    }
    
    #endregion
    
    #region Rumor Construction
    // -------------------------------------------------------------------------
    //  Rumor Construction
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Goes through Rumors list in inspector and binds them to a dictionary so they may be retrieved quickly.
    /// </summary>
    private void BuildRumorMap()
    {
        rumorMap = new Dictionary<string, LevelRumor>();
        foreach (var rumor in levelRumors)
        {
            if (string.IsNullOrEmpty(rumor.sceneName))
            {
                Debug.LogError("[RumorSystem] LevelRumor entry has empty sceneName. Skipping.", this);
                continue;
            }

            if (rumorMap.ContainsKey(rumor.sceneName))
            {
                Debug.LogError($"[RumorSystem] Duplicate sceneName '{rumor.sceneName}' found in LevelRumors. Using first entry.", this);
                continue;
            }

            rumorMap.Add(rumor.sceneName, rumor);
        }
    }
    
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
    
    /// <summary>
    /// Used to validate proper data is placed into Rumor fields.
    /// </summary>
    private void OnValidate()
    {
        // Validate data integrity in edit mode
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rumor in levelRumors)
        {
            if (string.IsNullOrEmpty(rumor.sceneName))
            {
                Debug.LogWarning("[RumorSystem] A LevelRumor entry is missing a sceneName.");
                continue;
            }

            if (!seen.Add(rumor.sceneName))
            {
                Debug.LogWarning($"[RumorSystem] Duplicate sceneName '{rumor.sceneName}' found in LevelRumors list.");
            }
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
    private void ApplyRumorToUI(LevelRumor rumor)
    {
        SetSprite(menuCharacterImage, rumor.characterPicture);
        ApplyTextProperties(menuCharacterTitle, rumor.characterTitle);
    
        ApplyTextProperties(menuCharacterBioText, rumor.characterBioText);
        SetSprite(menuCharacterBioImage, rumor.characterBioImage);
    
        ApplyTextProperties(menuCharacterTipText, rumor.characterTipText);
        SetSprite(menuCharacterTipImage, rumor.characterTipImage);
    
        ApplyTextProperties(menuCharacterRumorText, rumor.characterRumorText);
        SetSprite(menuCharacterRumorImage, rumor.characterRumorImage);
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
    private void ApplyTextProperties(TextMeshProUGUI target, RumorTextData source)
    {
        if (target == null || source == null) return;
    
        target.text = source.text;
        target.font = source.font;
        target.fontStyle = source.fontStyle;
        target.fontSize = source.fontSize;
        target.alignment = source.alignment;
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
    public void ShowRumor(string sceneName)
    {
        if (rumorMap.TryGetValue(sceneName, out var match))
        {
            ApplyRumorToUI(match);
        }
        else
        {
            Debug.LogWarning($"[RumorSystem] ShowRumor called with unknown scene '{sceneName}'.");
        }
    }
    
    #endregion
}
