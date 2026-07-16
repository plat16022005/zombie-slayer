using UnityEngine;

public enum BuffType
{
    MaxHP = 0,
    Attack = 1,
    Speed = 2,
    Defend = 3,
    FireRate = 4,
    MagnetRadius = 5,
    BonusExp = 6,
    Heal = 7,
    HpRegen = 8
}

[CreateAssetMenu(fileName = "NewCard", menuName = "Skill/CardData")]
public class CardData : ScriptableObject
{
    public string cardName;
    [TextArea(2, 4)]
    [Tooltip("Mô tả khi lần đầu nhận (Lv.1)")]
    public string description;
    
    [TextArea(2, 4)]
    [Tooltip("Mô tả riêng cho từng lần lặp lại (nâng cấp). Dòng 1 cho lúc lên Lv2, dòng 2 cho lúc lên Lv3... Nếu để trống sẽ dùng mô tả mặc định ở trên.")]
    public string[] upgradeDescriptions;
    
    public Sprite icon;
    
    [Header("Type Settings")]
    public bool isBuff; // true: Buff, false: Skill
    
    [Header("Buff Settings (if isBuff = true)")]
    public BuffType buffType; 
    public float buffAmount;
    [Tooltip("Tích vào đây nếu buffAmount là phần trăm (VD: 0.1 = 10%)")]
    public bool isPercentage;
    
    [Header("Skill Settings (if isBuff = false)")]
    public GameObject skillPrefab;

    [Header("Level Settings")]
    public int maxLevel = 5;
}
