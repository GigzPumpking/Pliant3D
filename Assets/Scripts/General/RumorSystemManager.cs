using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RumorSystemManager : MonoBehaviour
{
    #region Data
    // -------------------------------------------------------------------------
    //  Data
    // -------------------------------------------------------------------------
    
    [System.Serializable]
    public class LevelRumor
    {
        [Tooltip("Exact scene name (as it appears in Build Settings) that should trigger this rumor entry.")]
        public string sceneName;

        [Tooltip("Character Picture for Rumor")]
        public Sprite characterPicture;

        [Tooltip("Character info")]
        public string characterBio;

        [Tooltip("Character Tip")]
        public string characterTip;

        [Tooltip("Character Rumor")]
        public string characterRumor;
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
    [Tooltip("Reference to the menu character bio")]
    [SerializeField] private TextMeshProUGUI menuCharacterBio;
    [Tooltip("Reference to the menu character tip")]
    [SerializeField] private TextMeshProUGUI menuCharacterTip;
    [Tooltip("Reference to the menu character rumor")]
    [SerializeField] private TextMeshProUGUI menuCharacterRumor;

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
           ClearUI();
           return;
       }
       
       if (menuCharacterImage != null) menuCharacterImage.sprite = match.characterPicture;
       if (menuCharacterBio != null) menuCharacterBio.text = match.characterBio ?? string.Empty;
       if (menuCharacterTip != null) menuCharacterTip.text = match.characterTip ?? string.Empty;
       if (menuCharacterRumor != null) menuCharacterRumor.text = match.characterRumor ?? string.Empty;
    }
    
    #endregion
    
    #region Rumor Construction
    // -------------------------------------------------------------------------
    //  Rumor Construction
    // -------------------------------------------------------------------------
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
    
    private void BindUIReferences()
    {
        // If references aren't passed try to find elements
        if (menuCharacterImage == null)
            menuCharacterImage = sideMenuFolder?.transform.Find("CharacterPicture").GetComponent<Image>();
        if (menuCharacterBio == null)
            menuCharacterBio = sideMenuFolder?.transform.Find("CharacterBio").GetComponent<TextMeshProUGUI>();
        if (menuCharacterTip == null)
            menuCharacterTip = sideMenuFolder?.transform.Find("CharacterTip").GetComponent<TextMeshProUGUI>();
        if (menuCharacterRumor == null)
            menuCharacterRumor = sideMenuFolder?.transform.Find("CharacterRumor").GetComponent<TextMeshProUGUI>();

        if (sideMenuFolder != null &&
            (menuCharacterImage == null || menuCharacterBio == null
                                        || menuCharacterTip == null || menuCharacterRumor == null))
        {
            Debug.LogError("[RumorSystem] Failed to auto-bind one or more UI references. Please assign them in the inspector.");
        }
    }
    
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
    private void ClearUI()
    {
        if (menuCharacterImage != null) menuCharacterImage.sprite = null;
        if (menuCharacterBio != null) menuCharacterBio.text = string.Empty;
        if (menuCharacterTip != null) menuCharacterTip.text = string.Empty;
        if (menuCharacterRumor != null) menuCharacterRumor.text = string.Empty;
    }

    public void ShowRumor(string sceneName)
    {
        if (rumorMap.TryGetValue(sceneName, out var match))
        {
            if (menuCharacterImage != null) menuCharacterImage.sprite = match.characterPicture;
            if (menuCharacterBio != null) menuCharacterBio.text = match.characterBio ?? string.Empty;
            if (menuCharacterTip != null) menuCharacterTip.text = match.characterTip ?? string.Empty;
            if (menuCharacterRumor != null) menuCharacterRumor.text = match.characterRumor ?? string.Empty;
        }
        else
        {
            Debug.LogWarning($"[RumorSystem] ShowRumor called with unknown scene '{sceneName}'.");
        }
    }
    
    #endregion
}
