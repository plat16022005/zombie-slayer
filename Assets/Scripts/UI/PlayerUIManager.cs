using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager Instance { get; private set; }

    [Header("Target")]
    private Soldier player;
    
    [Header("HP UI")]
    [Tooltip("Thanh máu (Image có ImageType là Filled)")]
    [SerializeField] private Image hpFillImage;
    [Tooltip("Đoạn text hiện máu, vd: 100/100")]
    [SerializeField] private TextMeshProUGUI hpText;
    
    [Header("Ammo & Weapon UI")]
    [Tooltip("Đoạn text hiện đạn, vd: 30/30")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [Tooltip("Hình ảnh vũ khí hiện tại")]
    [SerializeField] private Image weaponIconImage;
    [Tooltip("Vòng tròn xám đếm ngược khi bắn/nạp đạn")]
    [SerializeField] private Image gunCooldownImage;
    [Tooltip("Số giây đếm ngược của vũ khí")]
    [SerializeField] private TextMeshProUGUI gunCooldownText;

    [Header("Skill Cooldown UI")]
    [Tooltip("Vòng tròn xám của chiêu Lựu Đạn (Image Fill)")]
    [SerializeField] private Image boomCooldownImage;
    [Tooltip("Số giây đếm ngược của chiêu Lựu Đạn")]
    [SerializeField] private TextMeshProUGUI boomCooldownText;
    
    [Tooltip("Vòng tròn xám của chiêu Lướt (Image Fill)")]
    [SerializeField] private Image dashCooldownImage;
    [Tooltip("Số giây đếm ngược của chiêu Lướt")]
    [SerializeField] private TextMeshProUGUI dashCooldownText;

    [Header("Boss Warning UI")]
    [Tooltip("Image màn hình mờ viền đỏ (Vignette) để cảnh báo Boss")]
    [SerializeField] private Image bossWarningOverlay;
    [Tooltip("Số lần nháy đỏ")]
    [SerializeField] private int warningFlashCount = 3;
    [Tooltip("Thời gian của 1 lần nháy (giây)")]
    [SerializeField] private float warningFlashDuration = 0.6f;
    [Tooltip("Âm thanh cảnh báo (vd: tiếng còi hú)")]
    [SerializeField] private AudioClip warningSound;

    [Header("BGM Manager")]
    [Tooltip("Nguồn phát nhạc nền của Game (Kéo Main Camera hoặc đối tượng chứa nhạc nền vào đây)")]
    [SerializeField] private AudioSource bgmAudioSource;

    [Header("Level Flow UI")]
    [Tooltip("Panel hiển thị mục tiêu của màn chơi lúc bắt đầu")]
    [SerializeField] private GameObject levelObjectivePanel;
    [SerializeField] private TextMeshProUGUI levelObjectiveText;
    
    [Tooltip("Panel chúc mừng chiến thắng")]
    [SerializeField] private GameObject victoryPanel;

    private AudioSource audioSource;
    private Coroutine warningRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Chuẩn bị AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Đảm bảo ban đầu tắt overlay
        if (bossWarningOverlay != null)
        {
            Color c = bossWarningOverlay.color;
            c.a = 0f;
            bossWarningOverlay.color = c;
            bossWarningOverlay.gameObject.SetActive(false);
        }

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

        // 3. Cập nhật Icon Súng
        if (weaponIconImage != null)
        {
            Sprite weaponSprite = player.GetCurrentWeaponSprite();
            if (weaponSprite != null)
            {
                weaponIconImage.sprite = weaponSprite;
                weaponIconImage.enabled = true;
            }
            else
            {
                weaponIconImage.enabled = false; // Ẩn icon nếu đang không cầm súng
            }
        }

        // 4. Cập nhật Cooldown Kỹ năng & Vũ khí
        UpdateSkillCooldown(boomCooldownImage, boomCooldownText, player.GetBoomCooldown(), player.GetBoomCooldownMax());
        UpdateSkillCooldown(dashCooldownImage, dashCooldownText, player.GetDashCooldown(), player.GetDashCooldownMax());
        
        // Cooldown súng (Ưu tiên hiện nạp đạn, nếu không nạp đạn thì hiện thời gian chờ bắn kế tiếp)
        // Lưu ý: Cooldown giữa 2 lần bắn (fireRate) thường rất ngắn (0.1s - 0.5s) nên vòng xoay sẽ rất lẹ. Còn Reload sẽ xoay chậm.
        UpdateSkillCooldown(gunCooldownImage, gunCooldownText, player.GetGunCooldown(), player.GetGunMaxCooldown());
    }

    private void UpdateSkillCooldown(Image fillImage, TextMeshProUGUI text, float currentCooldown, float maxCooldown)
    {
        if (fillImage != null)
        {
            if (currentCooldown > 0)
            {
                fillImage.fillAmount = currentCooldown / maxCooldown;
                fillImage.enabled = true; // Hiện vòng xám
            }
            else
            {
                fillImage.fillAmount = 0;
                fillImage.enabled = false; // Ẩn vòng xám khi đã hồi xong
            }
        }

        if (text != null)
        {
            if (currentCooldown > 0)
            {
                text.text = Mathf.CeilToInt(currentCooldown).ToString() + "s"; // Làm tròn lên: 4.2s thành 5s
                text.enabled = true;
            }
            else
            {
                text.text = "";
                text.enabled = false;
            }
        }
    }

    public void ShowBossWarning(AudioClip bossMusic = null)
    {
        if (warningSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(warningSound);
        }

        // Đổi nhạc nền qua UI Manager
        if (bossMusic != null && bgmAudioSource != null)
        {
            bgmAudioSource.clip = bossMusic;
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();
        }

        if (bossWarningOverlay == null) return;
        if (warningRoutine != null) StopCoroutine(warningRoutine);
        warningRoutine = StartCoroutine(BossWarningRoutine());
    }

    private System.Collections.IEnumerator BossWarningRoutine()
    {
        bossWarningOverlay.gameObject.SetActive(true);
        Color color = bossWarningOverlay.color;
        
        for (int i = 0; i < warningFlashCount; i++)
        {
            // Mờ dần thành đỏ (Fade in)
            float t = 0;
            while (t < warningFlashDuration / 2)
            {
                t += Time.deltaTime;
                color.a = Mathf.Lerp(0f, 0.6f, t / (warningFlashDuration / 2));
                bossWarningOverlay.color = color;
                yield return null;
            }
            
            // Nhạt dần đi (Fade out)
            t = 0;
            while (t < warningFlashDuration / 2)
            {
                t += Time.deltaTime;
                color.a = Mathf.Lerp(0.6f, 0f, t / (warningFlashDuration / 2));
                bossWarningOverlay.color = color;
                yield return null;
            }
        }
        
        color.a = 0f;
        bossWarningOverlay.color = color;
        bossWarningOverlay.gameObject.SetActive(false);
        warningRoutine = null;
    }

    public void ShowLevelObjective(string objectiveText, float duration = 8f)
    {
        if (levelObjectivePanel != null && levelObjectiveText != null)
        {
            Time.timeScale = 0f;
            levelObjectiveText.text = objectiveText;
            levelObjectivePanel.SetActive(true);
            StartCoroutine(HideObjectiveRoutine(duration));
        }
    }

    IEnumerator HideObjectiveRoutine(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        if (levelObjectivePanel != null) levelObjectivePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ShowVictoryPanel(string reason)
    {
        if (victoryPanel != null)
        {
            Time.timeScale = 0f; // Dừng thời gian
            victoryPanel.SetActive(true);
            
            // Tắt toàn bộ âm thanh trong game (nhạc nền, tiếng súng, tiếng zombie)
            AudioListener.pause = true;
        }
    }
    public void BackMenu()
    {
        Time.timeScale = 1f; // Phải reset lại thời gian trước khi chuyển scene
        AudioListener.pause = false; // Bật lại âm thanh cho scene sau
        SceneManager.LoadScene("MainMenu");
    }
}
