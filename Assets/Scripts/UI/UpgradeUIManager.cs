using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

public class UpgradeUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject upgradePanel; // Kéo Panel Nâng cấp vào đây

    [Header("Audio")]
    public AudioClip successSound;
    public AudioClip errorSound;
    private AudioSource audioSource;

    [Header("Cấu hình Tăng Chỉ Số / 1 Điểm")]
    public float attackPerPoint = 5f;
    public float hpPerPoint = 20f;
    public float defendPerPoint = 2f;
    public int goldPerPoint = 20;

    [Header("UI Text - Hiện tại (Gốc)")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI baseAttackText;
    public TextMeshProUGUI baseHpText;
    public TextMeshProUGUI baseDefendText;

    [Header("UI Text - Sau khi cộng")]
    public TextMeshProUGUI newAttackText;
    public TextMeshProUGUI newHpText;
    public TextMeshProUGUI newDefendText;

    [Header("UI Text - Thông tin khác")]
    public TextMeshProUGUI totalPointsText; // Hiển thị số điểm đang tiêu tốn
    public TextMeshProUGUI totalCostText;   // Hiển thị tổng vàng cần tiêu
    public TextMeshProUGUI warningText;     // Hiển thị cảnh báo (VD: Thiếu vàng)

    // Lưu trữ số điểm TẠM THỜI mà người chơi đang cộng thêm
    private int addedAttackPoints = 0;
    private int addedHpPoints = 0;
    private int addedDefendPoints = 0;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void OnEnable()
    {
        ResetPoints();
        UpdateUI();
    }

    public void ResetPoints()
    {
        addedAttackPoints = 0;
        addedHpPoints = 0;
        addedDefendPoints = 0;
        if (warningText != null) warningText.text = "";
        UpdateUI();
    }

    // ================= BUTTON ACTIONS ================= //

    public void IncreaseAttack()
    {
        addedAttackPoints++;
        UpdateUI();
    }

    public void DecreaseAttack()
    {
        if (addedAttackPoints > 0)
        {
            addedAttackPoints--;
            UpdateUI();
        }
    }

    public void IncreaseHp()
    {
        addedHpPoints++;
        UpdateUI();
    }

    public void DecreaseHp()
    {
        if (addedHpPoints > 0)
        {
            addedHpPoints--;
            UpdateUI();
        }
    }

    public void IncreaseDefend()
    {
        addedDefendPoints++;
        UpdateUI();
    }

    public void DecreaseDefend()
    {
        if (addedDefendPoints > 0)
        {
            addedDefendPoints--;
            UpdateUI();
        }
    }

    // ================= XỬ LÝ GIAO DIỆN ================= //

    private void UpdateUI()
    {
        if (DataGame.Instance == null) return;

        // Chỉ số gốc
        float baseAtk = DataGame.Instance.Attack;
        float baseHp = DataGame.Instance.MaxHp;
        float baseDef = DataGame.Instance.Defend;
        int currentGold = DataGame.Instance.Gold;

        // Chỉ số mới sau khi cộng tạm thời
        float newAtk = baseAtk + (addedAttackPoints * attackPerPoint);
        float newHp = baseHp + (addedHpPoints * hpPerPoint);
        float newDef = baseDef + (addedDefendPoints * defendPerPoint);

        // Chi phí
        int totalPoints = addedAttackPoints + addedHpPoints + addedDefendPoints;
        int totalCost = totalPoints * goldPerPoint;

        // Cập nhật Text Gốc
        if (baseAttackText != null) baseAttackText.text = baseAtk.ToString();
        if (baseHpText != null) baseHpText.text = baseHp.ToString();
        if (baseDefendText != null) baseDefendText.text = baseDef.ToString();
        if (goldText != null) goldText.text = currentGold.ToString();

        // Cập nhật Text Mới
        if (newAttackText != null) newAttackText.text = newAtk.ToString();
        if (newHpText != null) newHpText.text = newHp.ToString();
        if (newDefendText != null) newDefendText.text = newDef.ToString();

        // Cập nhật chi phí
        if (totalPointsText != null) totalPointsText.text = totalPoints.ToString();
        if (totalCostText != null) totalCostText.text = "-" +totalCost.ToString();

        // Cảnh báo Vàng
        if (warningText != null)
        {
            if (currentGold < totalCost)
            {
                int missingGold = totalCost - currentGold;
                warningText.text = $"<color=red>Không đủ Vàng! Còn thiếu {missingGold} Vàng.</color>";
            }
            else
            {
                warningText.text = ""; // Xóa cảnh báo nếu đủ tiền
            }
        }
    }

    // ================= XÁC NHẬN NÂNG CẤP ================= //

    public void ConfirmUpgrade()
    {
        if (DataGame.Instance == null) return;

        int totalPoints = addedAttackPoints + addedHpPoints + addedDefendPoints;
        if (totalPoints == 0)
        {
            if (warningText != null) warningText.text = "<color=yellow>Bạn chưa cộng điểm nào!</color>";
            PlaySound(errorSound);
            return;
        }

        int totalCost = totalPoints * goldPerPoint;

        if (DataGame.Instance.Gold < totalCost)
        {
            int missingGold = totalCost - DataGame.Instance.Gold;
            if (warningText != null) warningText.text = $"<color=red>Không đủ Vàng! Còn thiếu {missingGold} Vàng.</color>";
            PlaySound(errorSound);
            return;
        }

        // Tính toán chỉ số mới
        float newAtk = DataGame.Instance.Attack + (addedAttackPoints * attackPerPoint);
        float newHp = DataGame.Instance.MaxHp + (addedHpPoints * hpPerPoint);
        float newSpd = DataGame.Instance.Speed; // Giữ nguyên tốc độ
        float newDef = DataGame.Instance.Defend + (addedDefendPoints * defendPerPoint);
        int newGold = DataGame.Instance.Gold - totalCost;

        // Cập nhật vào Singleton DataGame
        DataGame.Instance.UpdateProfileData(
            DataGame.Instance.Username,
            newAtk,
            newHp,
            newSpd,
            newDef,
            newGold,
            DataGame.Instance.CurrentLevel
        );

        // Lưu lên Firestore
        SaveToFirestore(newAtk, newHp, newSpd, newDef, newGold);

        // Reset bộ đếm điểm tạm thời và cập nhật UI nội bộ
        ResetPoints();
        UpdateUI();
        
        // Cập nhật giao diện của MainMenu (hiển thị số vàng mới)
        MainMenuManager mainMenu = FindObjectOfType<MainMenuManager>();
        if (mainMenu != null) mainMenu.UpdateProfileUI();
        
        if (warningText != null) warningText.text = "<color=green>Nâng cấp thành công!</color>";
        PlaySound(successSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void SaveToFirestore(float attack, float maxHp, float speed, float defend, int gold)
    {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        if (auth != null && auth.CurrentUser != null)
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            string uid = auth.CurrentUser.UserId;

            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "attack", attack },
                { "maxHp", maxHp },
                { "speed", speed },
                { "defend", defend },
                { "gold", gold }
            };

            db.Collection("Users").Document(uid).UpdateAsync(updates).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) Debug.LogError("Lỗi khi lưu nâng cấp: " + task.Exception);
                else Debug.Log("Lưu nâng cấp thành công lên Firestore!");
            });
        }
    }
}
