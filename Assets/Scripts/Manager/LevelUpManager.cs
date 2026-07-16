using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject levelUpPanel;
    [Tooltip("3 Buttons for 3 card choices")]
    public Button[] cardButtons; 
    public Image[] cardIcons;
    public TextMeshProUGUI[] cardNames;
    public TextMeshProUGUI[] cardDescriptions;
    public TextMeshProUGUI[] cardLevels;

    [Header("Card Backgrounds")]
    [Tooltip("Khung viền/nền cho thẻ Buff")]
    public Sprite buffCardBackground;
    [Tooltip("Khung viền/nền cho thẻ Skill")]
    public Sprite skillCardBackground;

    [Header("Skill Spawning")]
    [Tooltip("Kéo GameObject mà bạn muốn làm nơi xuất hiện của các Kỹ năng vào đây.")]
    public GameObject targetSpawnObject;

    [Header("Data")]
    [Tooltip("Kéo thả tất cả các CardData (Skill và Buff) vào đây")]
    public List<CardData> allCards;

    [Header("Limits")]
    [Tooltip("Số lượng tối đa thẻ Kỹ năng (Skill) người chơi có thể sở hữu")]
    public int maxSkillSlots = 3;
    [Tooltip("Số lượng tối đa thẻ Buff người chơi có thể sở hữu")]
    public int maxBuffSlots = 3;

    private Dictionary<CardData, int> activeCards = new Dictionary<CardData, int>();
    private Dictionary<CardData, GameObject> activeSkills = new Dictionary<CardData, GameObject>();

    private int currentSkillCount = 0;
    private int currentBuffCount = 0;
    
    private int pendingLevelUps = 0;
    private List<CardData> currentChoices = new List<CardData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
    }

    public void ShowLevelUpUI()
    {
        pendingLevelUps++;
        if (levelUpPanel != null && !levelUpPanel.activeSelf)
        {
            ShowNextLevelUp();
        }
    }

    private void ShowNextLevelUp()
    {
        if (pendingLevelUps <= 0)
        {
            levelUpPanel.SetActive(false);
            Time.timeScale = 1f;
            return;
        }
        
        pendingLevelUps--;
        Time.timeScale = 0f;
        levelUpPanel.SetActive(true);
        GenerateChoices();
    }

    private void GenerateChoices()
    {
        List<CardData> available = new List<CardData>();
        
        foreach (var card in allCards)
        {
            int level = activeCards.ContainsKey(card) ? activeCards[card] : 0;
            
            // Nếu đã max level thì bỏ qua
            if (level >= card.maxLevel) continue;

            if (level == 0)
            {
                // Nếu chưa có, kiểm tra giới hạn slot
                if (card.isBuff && currentBuffCount >= maxBuffSlots) continue;
                if (!card.isBuff && currentSkillCount >= maxSkillSlots) continue;
            }

            available.Add(card);
        }

        // --- FALLBACK LOGIC ---
        // Nếu không còn thẻ nào khả dụng (tất cả đều đã max level)
        if (available.Count == 0)
        {
            CardData fallbackCard = ScriptableObject.CreateInstance<CardData>();
            fallbackCard.cardName = "Cứu Thương";
            fallbackCard.description = "Hồi phục 30% Máu tối đa (Thẻ dự phòng khi đã Max Level mọi thứ).";
            fallbackCard.isBuff = true;
            fallbackCard.buffType = BuffType.Heal;
            fallbackCard.buffAmount = 0.3f;
            fallbackCard.isPercentage = true;
            fallbackCard.maxLevel = 9999;
            available.Add(fallbackCard);
        }

        // Trộn mảng available
        for (int i = 0; i < available.Count; i++)
        {
            CardData temp = available[i];
            int randomIndex = Random.Range(i, available.Count);
            available[i] = available[randomIndex];
            available[randomIndex] = temp;
        }

        currentChoices.Clear();
        for (int i = 0; i < 3; i++)
        {
            if (i < available.Count)
            {
                currentChoices.Add(available[i]);
                cardButtons[i].gameObject.SetActive(true);
                UpdateCardUI(i, available[i]);
            }
            else
            {
                cardButtons[i].gameObject.SetActive(false); // Ẩn bớt card nếu không đủ 3 lựa chọn
            }
        }
        
        // Nếu không có lựa chọn nào, tự động đóng (hoặc có thể code cho mặc định heal máu ở đây)
        if (currentChoices.Count == 0)
        {
            ShowNextLevelUp();
        }
    }

    private void UpdateCardUI(int index, CardData card)
    {
        int level = activeCards.ContainsKey(card) ? activeCards[card] : 0;
        int nextLevel = level + 1;
        
        if (cardIcons[index] != null) cardIcons[index].sprite = card.icon;
        if (cardNames[index] != null) cardNames[index].text = card.cardName;

        // Xử lý hiển thị mô tả dựa trên cấp độ
        string currentDesc = card.description;
        if (level > 0 && card.upgradeDescriptions != null && card.upgradeDescriptions.Length >= level)
        {
            // level = 1 (tức là sắp lên Lv2) -> lấy phần tử 0
            if (!string.IsNullOrEmpty(card.upgradeDescriptions[level - 1]))
            {
                currentDesc = card.upgradeDescriptions[level - 1];
            }
        }
        
        if (cardDescriptions[index] != null) cardDescriptions[index].text = currentDesc;
        if (cardLevels[index] != null) cardLevels[index].text = level == 0 ? "New!" : $"Lv.{nextLevel}";
        
        // Thay đổi background của thẻ dựa vào loại
        if (cardButtons[index] != null)
        {
            Image bg = cardButtons[index].GetComponent<Image>();
            if (bg != null)
            {
                if (card.isBuff && buffCardBackground != null)
                    bg.sprite = buffCardBackground;
                else if (!card.isBuff && skillCardBackground != null)
                    bg.sprite = skillCardBackground;
            }
        }

        // Remove old listeners and add new
        cardButtons[index].onClick.RemoveAllListeners();
        cardButtons[index].onClick.AddListener(() => OnCardSelected(card));
    }

    private void OnCardSelected(CardData card)
    {
        int level = activeCards.ContainsKey(card) ? activeCards[card] : 0;
        
        if (level == 0)
        {
            // Nhận mới
            activeCards[card] = 1;
            if (card.isBuff)
            {
                currentBuffCount++;
                ApplyBuff(card);
            }
            else
            {
                currentSkillCount++;
                SpawnSkill(card);
            }
        }
        else
        {
            // Nâng cấp
            activeCards[card]++;
            if (card.isBuff)
            {
                ApplyBuff(card);
            }
            else
            {
                LevelUpSkill(card);
            }
        }

        // Show next if there are pending level ups
        ShowNextLevelUp();
    }
    
    private void ApplyBuff(CardData card)
    {
        Soldier player = FindObjectOfType<Soldier>();
        if (player == null) return;

        switch (card.buffType)
        {
            case BuffType.MaxHP: 
                player.AddMaxHp(card.buffAmount, card.isPercentage); 
                break;
            case BuffType.Heal: 
                player.Heal(card.buffAmount, card.isPercentage); 
                break;
            case BuffType.HpRegen:
                player.AddHpRegen(card.buffAmount);
                break;
            case BuffType.Attack: 
                player.AddAttack(card.buffAmount, card.isPercentage); 
                break;
            case BuffType.Speed: 
                player.AddSpeed(card.buffAmount, card.isPercentage); 
                break;
            case BuffType.Defend: 
                player.AddDefend(card.buffAmount, card.isPercentage); 
                break;
            case BuffType.FireRate:
                if (card.isPercentage) player.fireSpeed += 1f * card.buffAmount; // Gốc là 1
                else player.fireSpeed += card.buffAmount;
                break;
            case BuffType.MagnetRadius:
                player.IncreaseMagnetRadius(card.buffAmount, card.isPercentage);
                break;
            case BuffType.BonusExp:
                if (card.isPercentage) player.bonusExpMultiplier += 1f * card.buffAmount; // Gốc là 1
                else player.bonusExpMultiplier += card.buffAmount;
                break;
        }
    }

    private void SpawnSkill(CardData card)
    {
        Soldier player = FindObjectOfType<Soldier>();
        if (player == null || card.skillPrefab == null) return;

        // Xác định parent để chứa Skill
        Transform parentTransform = player.transform;
        
        if (targetSpawnObject != null)
        {
            parentTransform = targetSpawnObject.transform;
        }
        else if (player.skillHolder != null)
        {
            parentTransform = player.skillHolder;
        }

        // Sinh ra prefab của skill và set làm con của vị trí đã chọn
        GameObject skillInstance = Instantiate(card.skillPrefab, parentTransform.position, Quaternion.identity);
        skillInstance.transform.SetParent(parentTransform); 
        
        activeSkills[card] = skillInstance;
    }

    private void LevelUpSkill(CardData card)
    {
        if (activeSkills.ContainsKey(card))
        {
            GameObject skillObj = activeSkills[card];
            if (skillObj != null)
            {
                // Gọi hàm LevelUp() trên tất cả các component của prefab đó
                skillObj.SendMessage("LevelUp", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
