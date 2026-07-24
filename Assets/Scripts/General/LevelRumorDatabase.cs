using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelRumorDatabase", menuName = "Rumor System/Level Rumor Database")]
public class LevelRumorDatabase : ScriptableObject
{
    [System.Serializable]
    public class RumorTextData
    {
        [Tooltip("The text content to display.")] [TextArea(5, 10)]
        [SerializeField] private string text = string.Empty;
        
        [Tooltip("Font asset to use for this text.")]
        [SerializeField] private TMP_FontAsset font;
        
        [Tooltip("Font style to apply to the text")]
        [SerializeField] private FontStyles fontStyle = FontStyles.Normal;
        
        [Tooltip("Font size for this text.")]
        [SerializeField] private float fontSize = 36f;
        
        [Tooltip("Text alignment.")]
        [SerializeField] private TextAlignmentOptions alignment = TextAlignmentOptions.Center;

        public string Text => text;
        public TMP_FontAsset Font => font;
        public FontStyles FontStyle => fontStyle;
        public float FontSize => fontSize;
        public TextAlignmentOptions Alignment => alignment;
    }

    [System.Serializable]
    public class LevelRumor
    {
        [Tooltip("The level id that this rumor is tied to load from such as level 1.")]
        [SerializeField] private LevelId levelId;

        [Tooltip("Character Picture for Rumor")]
        [SerializeField] private Sprite characterPicture;
        
        [Tooltip("Character Title for Rumor")]
        [SerializeField] private RumorTextData characterTitle;

        [Tooltip("Character info text")]
        [SerializeField] private RumorTextData characterBioText;
        
        [Tooltip("Character Bio Background Image for Rumor")]
        [SerializeField] private Sprite characterBioImage;

        [Tooltip("Character Tip text")]
        [SerializeField] private RumorTextData characterTipText;
        
        [Tooltip("Character Tip Background Image for Rumor")]
        [SerializeField] private Sprite characterTipImage;

        [Tooltip("Character Rumor text")]
        [SerializeField] private RumorTextData characterRumorText;
        
        [Tooltip("Character Rumor Background Image for Rumor")]
        [SerializeField] private Sprite characterRumorImage;

        // Properties for external access
        public LevelId LevelId => levelId;
        public Sprite CharacterPicture => characterPicture;
        public RumorTextData CharacterTitle => characterTitle;
        public RumorTextData CharacterBioText => characterBioText;
        public Sprite CharacterBioImage => characterBioImage;
        public RumorTextData CharacterTipText => characterTipText;
        public Sprite CharacterTipImage => characterTipImage;
        public RumorTextData CharacterRumorText => characterRumorText;
        public Sprite CharacterRumorImage => characterRumorImage;
    }

    [Header("Level Rumors")]
    [Tooltip("Each entry maps a level/scene to the rumor that shows when that level loads.")]
    [SerializeField] private List<LevelRumor> rumors = new List<LevelRumor>();
    
    // O(1) lookup dictionary
    private Dictionary<LevelId, LevelRumor> rumorMap;
    
    /// <summary>
    /// Tries to get a rumor by level ID. Returns true if found.
    /// </summary>
    public bool TryGetRumor(LevelId levelId, out LevelRumor rumor)
    {
        if (rumorMap == null)
            BuildRumorMap();
            
        return rumorMap.TryGetValue(levelId, out rumor);
    }
    
    /// <summary>
    /// Goes through Rumors list in inspector and binds them to a dictionary so they may be retrieved quickly.
    /// </summary>
    private void BuildRumorMap()
    {
        rumorMap = new Dictionary<LevelId, LevelRumor>();
        foreach (var rumor in rumors)
        {
            if (rumorMap.ContainsKey(rumor.LevelId))
            {
                Debug.LogError($"[RumorSystem] Duplicate levelId '{rumor.LevelId}' found in LevelRumors. Using first entry.", this);
                continue;
            }

            rumorMap.Add(rumor.LevelId, rumor);
        }
    }
    
    private void OnValidate()
    {
        // Validate data integrity in edit mode
        var seen = new HashSet<LevelId>();
        foreach (var rumor in rumors)
        {
            if (!seen.Add(rumor.LevelId))
            {
                Debug.LogWarning($"[RumorSystem] Duplicate levelId '{rumor.LevelId}' found in LevelRumors list.");
            }
        }
    }
}
