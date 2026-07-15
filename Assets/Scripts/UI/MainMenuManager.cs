using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject PanelStatus;
    [Header("UI Name Input")]
    [SerializeField] private GameObject nameInputPanel;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button submitNameButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [Header("Profile Stats")]
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI coinText;

    [Header("Level Selection UI")]
    [SerializeField] private Transform levelsContent;

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string currentUserId;

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        if (auth.CurrentUser != null)
        {
            currentUserId = auth.CurrentUser.UserId;
            CheckUserData();
        }
        else
        {
            Debug.LogError("No user is logged in!");
        }

        if (submitNameButton != null)
        {
            submitNameButton.onClick.AddListener(OnSubmitName);
        }
    }

    private void CheckUserData()
    {
        // Ẩn panel nhập tên trước
        if (nameInputPanel != null) nameInputPanel.SetActive(false);

        if (DataGame.Instance != null)
        {
            if (DataGame.Instance.HasProfile)
            {
                Debug.Log($"Welcome back, {DataGame.Instance.Username}!");
                if (usernameText != null) usernameText.text = DataGame.Instance.Username;
                if (coinText != null) coinText.text = DataGame.Instance.Gold.ToString();
                
                SetupLevelButtons();
            }
            else
            {
                Debug.Log("New user detected (No profile in DataGame), opening name input panel.");
                if (nameInputPanel != null) nameInputPanel.SetActive(true);
            }
        }
        else
        {
            Debug.LogError("DataGame Singleton không tồn tại! Hãy chắc chắn đã chạy từ Scene Loading.");
        }
    }

    private void OnSubmitName()
    {
        string username = nameInputField.text.Trim();
        if (string.IsNullOrEmpty(username))
        {
            SetStatus("Tên không được để trống!", Color.red);
            return;
        }

        if (username.Length < 3)
        {
            SetStatus("Tên phải có ít nhất 3 ký tự!", Color.red);
            return;
        }

        // Tạm khóa nút để tránh spam
        submitNameButton.interactable = false;
        SetStatus("Đang kiểm tra tên...", Color.yellow);

        DocumentReference usernameDoc = db.Collection("Usernames").Document(username);
        
        usernameDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                SetStatus("Lỗi kết nối. Vui lòng thử lại!", Color.red);
                submitNameButton.interactable = true;
                return;
            }

            DocumentSnapshot snapshot = task.Result;
            if (snapshot.Exists)
            {
                SetStatus("Tên đã tồn tại, vui lòng chọn tên khác!", Color.red);
                submitNameButton.interactable = true;
            }
            else
            {
                // Tên hợp lệ, tiến hành tạo data
                CreateUserData(username);
            }
        });
    }

    private void CreateUserData(string username)
    {
        SetStatus("Đang khởi tạo nhân vật...", Color.yellow);

        // 1. Tạo thông tin cơ bản cho User
        float initAttack = 20f;
        float initMaxHp = 100f;
        float initSpeed = 10f;
        float initDefend = 5f;
        int initGold = 0;
        int initLevel = 1; // Màn khởi đầu

        Dictionary<string, object> userData = new Dictionary<string, object>
        {
            { "username", username },
            { "attack", initAttack },
            { "maxHp", initMaxHp },
            { "speed", initSpeed },
            { "defend", initDefend },
            { "gold", initGold },
            { "currentLevel", initLevel }
        };

        // 2. Ghi nhận username để chống trùng lặp
        Dictionary<string, object> usernameData = new Dictionary<string, object>
        {
            { "uid", currentUserId }
        };

        // 3. (Tùy chọn) Ghi riêng 1 Collection CurrentLevel theo ý bạn
        Dictionary<string, object> levelData = new Dictionary<string, object>
        {
            { "level", initLevel }
        };

        DocumentReference userDoc = db.Collection("Users").Document(currentUserId);
        DocumentReference usernameDoc = db.Collection("Usernames").Document(username);
        DocumentReference levelDoc = db.Collection("CurrentLevel").Document(currentUserId);

        // Dùng Batch để ghi đồng thời, đảm bảo tính toàn vẹn dữ liệu
        WriteBatch batch = db.StartBatch();
        batch.Set(userDoc, userData);
        batch.Set(usernameDoc, usernameData);
        batch.Set(levelDoc, levelData);

        batch.CommitAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                SetStatus("Tạo nhân vật thất bại!", Color.red);
                submitNameButton.interactable = true;
                Debug.LogError("Batch write failed: " + task.Exception);
            }
            else
            {
                SetStatus("Tạo thành công!", Color.green);
                Debug.Log("User data created successfully.");
                
                // Cập nhật vào Singleton DataGame
                if (DataGame.Instance != null)
                {
                    DataGame.Instance.UpdateProfileData(username, initAttack, initMaxHp, initSpeed, initDefend, initGold, initLevel);
                }

                // Ẩn panel nhập tên
                if (nameInputPanel != null) nameInputPanel.SetActive(false);
                if (usernameText != null) usernameText.text = DataGame.Instance.Username;
                if (coinText != null) coinText.text = DataGame.Instance.Gold.ToString();
                
                SetupLevelButtons();
            }
        });
    }

    private void SetupLevelButtons()
    {
        if (levelsContent == null) return;
        
        int currentLevel = 1;
        if (DataGame.Instance != null && DataGame.Instance.HasProfile)
        {
            currentLevel = DataGame.Instance.CurrentLevel;
        }

        // Lặp qua tất cả các button level (Lv1, Lv2,...)
        for (int i = 0; i < levelsContent.childCount; i++)
        {
            Transform levelBtn = levelsContent.GetChild(i);
            int levelOfButton = i + 1; // Nút đầu tiên là Level 1

            Transform bgTransform = levelBtn.Find("Bg");
            Transform lockTransform = levelBtn.Find("Lock");

            if (bgTransform == null || lockTransform == null) continue;

            // Lấy toàn bộ component Image nằm trong Bg (kể cả các child của Bg)
            Image[] bgImages = bgTransform.GetComponentsInChildren<Image>(true);

            if (levelOfButton < currentLevel)
            {
                // Màn đã qua: Ảnh nền Trắng, ẩn Khóa
                SetImagesColor(bgImages, Color.white);
                lockTransform.gameObject.SetActive(false);
            }
            else if (levelOfButton == currentLevel)
            {
                // Màn hiện tại: Ảnh nền Xám đậm, ẩn Khóa
                SetImagesColor(bgImages, new Color(0.4f, 0.4f, 0.4f, 1f));
                lockTransform.gameObject.SetActive(false);
            }
            else
            {
                // Màn chưa tới: Ảnh nền Đen, hiện Khóa
                SetImagesColor(bgImages, Color.black);
                lockTransform.gameObject.SetActive(true);
            }

            Button btn = levelBtn.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                if (levelOfButton <= currentLevel)
                {
                    btn.onClick.AddListener(() => StartThisLevel("Level" + levelOfButton));
                }
            }
        }
    }

    private void SetImagesColor(Image[] images, Color color)
    {
        foreach (Image img in images)
        {
            img.color = color;
        }
    }

    private void SetStatus(string msg, Color color)
    {
        PanelStatus.SetActive(true);
        if (statusText != null)
        {
            statusText.text = msg;
            statusText.color = color;
        }
    }
    public void StartThisLevel(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
