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
    
    [Header("Exp UI")]
    [Tooltip("Thanh kinh nghiệm (Image có ImageType là Filled)")]
    [SerializeField] private Image expFillImage;
    [Tooltip("Đoạn text hiện cấp độ (Vd: Lv.1)")]
    [SerializeField] private TextMeshProUGUI levelText;
    
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

    [Header("Boss HP UI")]
    [Tooltip("Panel chứa toàn bộ thanh máu Boss")]
    [SerializeField] private GameObject bossHealthPanel;
    [Tooltip("Hình ảnh thanh máu Boss (Image Filled)")]
    [SerializeField] private Image bossHpFillImage;
    [Tooltip("Dòng Text hiện máu Boss (VD: 1000/1000)")]
    [SerializeField] private TextMeshProUGUI bossHpText;
    [Tooltip("Dòng Text hiện tên Boss")]
    [SerializeField] private TextMeshProUGUI bossNameText;
    [Tooltip("Hình ảnh Avatar của Boss")]
    [SerializeField] private Image bossAvatarImage;

    [Header("BGM Manager")]
    [Tooltip("Nguồn phát nhạc nền của Game (Kéo Main Camera hoặc đối tượng chứa nhạc nền vào đây)")]
    [SerializeField] private AudioSource bgmAudioSource;

    [Header("Level Flow UI")]
    [Tooltip("Panel hiển thị mục tiêu của màn chơi lúc bắt đầu")]
    [SerializeField] private GameObject levelObjectivePanel;
    [SerializeField] private TextMeshProUGUI levelObjectiveText;
    
    [Tooltip("Panel chúc mừng chiến thắng")]
    [SerializeField] private GameObject victoryPanel;
    [Tooltip("Âm thanh phát ra khi chiến thắng")]
    [SerializeField] private AudioClip victorySound;
    
    [Tooltip("Panel thua cuộc (Game Over)")]
    [SerializeField] private GameObject gameOverPanel;
    [Tooltip("Âm thanh phát ra khi thua cuộc")]
    [SerializeField] private AudioClip gameOverSound;
    
    [Tooltip("Dòng Text hiện số vàng nhận được trong bảng Victory")]
    [SerializeField] private TextMeshProUGUI victoryGoldText;

    [Header("Level Progress UI")]
    [Tooltip("Text hiển thị tiến trình: Thời gian, Wave, hoặc Boss HP trên cùng màn hình")]
    [SerializeField] private TextMeshProUGUI levelProgressText;

    [Header("Settings UI")]
    [Tooltip("Panel Cài đặt")]
    [SerializeField] private GameObject settingPanel;
    [Tooltip("Slider chỉnh âm lượng Nhạc nền (BGM)")]
    [SerializeField] private UnityEngine.UI.Slider bgmSlider;
    [Tooltip("Slider chỉnh âm lượng Hiệu ứng (SFX)")]
    [SerializeField] private UnityEngine.UI.Slider sfxSlider;

    private AudioSource audioSource;
    private Coroutine warningRoutine;
    private Coroutine bloodRoutine;

    [Header("Player Blood UI")]
    [Tooltip("Image màn hình mờ viền đỏ khi bị thương")]
    [SerializeField] private Image playerBloodOverlay;
    
    private float originalBloodAlpha;

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
        audioSource.ignoreListenerPause = true; // Bỏ qua việc Pause game để vẫn phát tiếng win/lose

        // Đảm bảo ban đầu tắt overlay và thanh máu Boss
        if (bossWarningOverlay != null)
        {
            Color c = bossWarningOverlay.color;
            c.a = 0f;
            bossWarningOverlay.color = c;
            bossWarningOverlay.gameObject.SetActive(false);
        }

        if (bossHealthPanel != null)
        {
            bossHealthPanel.SetActive(false);
        }

        if (playerBloodOverlay != null)
        {
            originalBloodAlpha = playerBloodOverlay.color.a; // Lưu lại alpha gốc
            Color c = playerBloodOverlay.color;
            c.a = 0f;
            playerBloodOverlay.color = c;
            playerBloodOverlay.gameObject.SetActive(false);
        }

        // Tự động quét tìm component Soldier trong màn chơi
        player = FindObjectOfType<Soldier>();
        
        if (player == null)
        {
            // Dự phòng: Tìm theo tag "Player" nếu FindObjectOfType thất bại
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.GetComponent<Soldier>();
        }

        // --- HỆ THỐNG ÂM THANH (SETTINGS) ---
        // Tách biệt nhạc nền (BGM) ra khỏi bộ đệm tổng (AudioListener)
        // Nhờ vậy, AudioListener.volume sẽ CHỈ làm nhỏ tiếng súng, tiếng quái (SFX)
        if (bgmAudioSource != null)
        {
            bgmAudioSource.ignoreListenerVolume = true; 
        }

        // Tải âm lượng từ PlayerPrefs (lưu trên máy), mặc định là 1 (Max)
        float savedBGM = PlayerPrefs.GetFloat("BGM_Volume", 1f);
        float savedSFX = PlayerPrefs.GetFloat("SFX_Volume", 1f);

        if (bgmAudioSource != null) bgmAudioSource.volume = savedBGM;
        AudioListener.volume = savedSFX; // Âm lượng tổng = SFX

        // Cập nhật giá trị vào Slider và bắt sự kiện kéo thanh trượt
        if (bgmSlider != null)
        {
            bgmSlider.value = savedBGM;
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }
        
        if (sfxSlider != null)
        {
            sfxSlider.value = savedSFX;
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
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

    public void ShowBloodVignette()
    {
        if (playerBloodOverlay == null) return;
        
        if (bloodRoutine != null) StopCoroutine(bloodRoutine); 
        bloodRoutine = StartCoroutine(BloodVignetteRoutine());
    }

    private System.Collections.IEnumerator BloodVignetteRoutine()
    {
        playerBloodOverlay.gameObject.SetActive(true);
        Color color = playerBloodOverlay.color;
        
        float duration = 0.15f;
        
        // Mờ dần tới alpha gốc (Fade in)
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(0f, originalBloodAlpha, t / duration);
            playerBloodOverlay.color = color;
            yield return null;
        }
        
        // Nhạt dần đi về 0 (Fade out)
        t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(originalBloodAlpha, 0f, t / duration);
            playerBloodOverlay.color = color;
            yield return null;
        }
        
        color.a = 0f;
        playerBloodOverlay.color = color;
        playerBloodOverlay.gameObject.SetActive(false);
        bloodRoutine = null;
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

    public void ShowVictoryPanel(string reason, int earnedGold = 0)
    {
        if (victoryPanel != null)
        {
            Time.timeScale = 0f; // Dừng thời gian
            victoryPanel.SetActive(true);
            
            if (victoryGoldText != null)
            {
                if (earnedGold > 0)
                    victoryGoldText.text = $"+{earnedGold} Vàng";
                else
                    victoryGoldText.text = "0 Vàng";
            }
            
            // Tắt toàn bộ âm thanh trong game (nhạc nền, tiếng súng, tiếng zombie)
            AudioListener.pause = true;
            if (bgmAudioSource != null) bgmAudioSource.Pause(); // Vì BGM đã tách khỏi Listener nên phải Pause thủ công
            
            // Phát âm thanh chiến thắng
            if (victorySound != null && audioSource != null)
            {
                audioSource.PlayOneShot(victorySound);
            }
        }
    }

    public void ShowGameOverPanel(float delay = 0f)
    {
        StartCoroutine(GameOverRoutine(delay));
    }

    private IEnumerator GameOverRoutine(float delay)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        if (gameOverPanel != null)
        {
            Time.timeScale = 0f; // Dừng thời gian
            gameOverPanel.SetActive(true);
            
            // Tắt âm thanh khi game over
            AudioListener.pause = true;
            if (bgmAudioSource != null) bgmAudioSource.Pause();

            // Phát âm thanh thua cuộc
            if (gameOverSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(gameOverSound);
            }
        }
    }

    // ============================================
    // CÁC HÀM XỬ LÝ SETTING (KÉO VÀO BUTTON/SLIDER TRONG UNITY)
    // ============================================

    public void OpenSettingPanel()
    {
        if (settingPanel != null)
        {
            settingPanel.SetActive(true);
            Time.timeScale = 0f; // Dừng thời gian
        }
    }

    public void CloseSettingPanel()
    {
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
            
            // Chỉ khôi phục thời gian nếu người chơi chưa thắng hay chưa mở cái bảng nào khác đè lên
            if ((victoryPanel == null || !victoryPanel.activeSelf) && (levelObjectivePanel == null || !levelObjectivePanel.activeSelf))
            {
                Time.timeScale = 1f;
            }
        }
    }

    public void SetBGMVolume(float volume)
    {
        if (bgmAudioSource != null) bgmAudioSource.volume = volume;
        PlayerPrefs.SetFloat("BGM_Volume", volume); // Lưu lại trên máy
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("SFX_Volume", volume); // Lưu lại trên máy
        PlayerPrefs.Save();
    }

    public void BackMenu()
    {
        Time.timeScale = 1f; // Phải reset lại thời gian trước khi chuyển scene
        AudioListener.pause = false; // Bật lại âm thanh cho scene sau
        SceneManager.LoadScene("MainMenu");
    }

    public void UpdateLevelProgress(string text)
    {
        if (levelProgressText != null)
        {
            if (string.IsNullOrEmpty(text))
            {
                levelProgressText.gameObject.SetActive(false);
            }
            else
            {
                levelProgressText.text = text;
                if (!levelProgressText.gameObject.activeSelf)
                    levelProgressText.gameObject.SetActive(true);
            }
        }
    }

    public void UpdateBossHealth(float currentHp, float maxHp, string bossName = "", Sprite bossAvatar = null)
    {
        if (bossHealthPanel != null)
        {
            if (!bossHealthPanel.activeSelf)
            {
                bossHealthPanel.SetActive(true);
                
                // Cập nhật tên và avatar một lần khi thanh máu xuất hiện
                if (bossNameText != null && !string.IsNullOrEmpty(bossName))
                    bossNameText.text = bossName;
                    
                if (bossAvatarImage != null && bossAvatar != null)
                {
                    bossAvatarImage.sprite = bossAvatar;
                    bossAvatarImage.enabled = true;
                }
            }

            if (bossHpFillImage != null)
            {
                // Tránh lỗi chia cho 0
                maxHp = maxHp > 0 ? maxHp : 1f;
                bossHpFillImage.fillAmount = currentHp / maxHp;
            }

            if (bossHpText != null)
                bossHpText.text = $"{Mathf.Max(0, Mathf.CeilToInt(currentHp))} / {Mathf.CeilToInt(maxHp)}";
        }
    }

    public void HideBossHealth()
    {
        if (bossHealthPanel != null && bossHealthPanel.activeSelf)
        {
            bossHealthPanel.SetActive(false);
        }
    }

    public void UpdateExpUI(int currentExp, int maxExp, int level)
    {
        if (expFillImage != null)
        {
            maxExp = maxExp > 0 ? maxExp : 1;
            expFillImage.fillAmount = (float)currentExp / maxExp;
        }
        
        if (levelText != null)
        {
            levelText.text = $"Lv.{level}";
        }
    }
}
