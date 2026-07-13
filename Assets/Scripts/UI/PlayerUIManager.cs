using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIManager : MonoBehaviour
{
    [Header("Target")]
    private Soldier player;
    
    [Header("HP UI")]
    [Tooltip("Thanh máu (Image có ImageType là Filled)")]
    [SerializeField] private Image hpFillImage;
    [Tooltip("Đoạn text hiện máu, vd: 100/100")]
    [SerializeField] private TextMeshProUGUI hpText;
    
    [Header("Ammo UI")]
    [Tooltip("Đoạn text hiện đạn, vd: 30/30")]
    [SerializeField] private TextMeshProUGUI ammoText;
    
    private void Awake()
    {
        // Tự động quét tìm component Soldier trong màn chơi
        player = FindObjectOfType<Soldier>();
        
        if (player == null)
        {
            // Dự phòng: Tìm theo tag "Player" nếu FindObjectOfType thất bại
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.GetComponent<Soldier>();
        }
    }

    private void Update()
    {
        if (player == null) return;
        
        // 1. Cập nhật Máu
        float currentHp = Mathf.Max(0, player.hp);
        float maxHp = player.maxHp > 0 ? player.maxHp : 100f; // Phòng hờ lỗi chia cho 0
        
        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = currentHp / maxHp;
        }
            
        if (hpText != null)
        {
            hpText.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
        }
            
        // 2. Cập nhật Đạn
        if (ammoText != null)
        {
            int currentAmmo = player.GetCurrentAmmo();
            int maxAmmo = player.GetMaxAmmo();
            
            // Nếu có súng (maxAmmo > 0) thì hiện số, nếu tay không thì hiện dấu gạch
            if (maxAmmo > 0)
            {
                ammoText.text = $"{currentAmmo} / {maxAmmo}";
            }
            else
            {
                ammoText.text = "- / -";
            }
        }
    }
}
