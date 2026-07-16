using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;

public class DataGame : MonoBehaviour
{
    public static DataGame Instance { get; private set; }

    public bool HasProfile { get; private set; } = false;
    
    // Thông số cơ bản
    public string Username { get; private set; }
    public float Attack { get; private set; }
    public float MaxHp { get; private set; }
    public float Speed { get; private set; }
    public float Defend { get; private set; }
    public int Gold { get; private set; }
    public int CurrentLevel { get; private set; }

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
        }
    }

    /// <summary>
    /// Fetch dữ liệu từ Firestore dựa trên UID hiện tại.
    /// Gọi callback sau khi hoàn thành.
    /// </summary>
    public void LoadUserData(Action onComplete)
    {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        if (auth.CurrentUser == null)
        {
            Debug.LogError("Chưa đăng nhập, không thể tải dữ liệu.");
            HasProfile = false;
            onComplete?.Invoke();
            return;
        }

        string uid = auth.CurrentUser.UserId;
        DocumentReference userDoc = db.Collection("Users").Document(uid);

        userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Lỗi khi tải dữ liệu người chơi: " + task.Exception);
                HasProfile = false;
                onComplete?.Invoke();
            }
            else
            {
                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    Dictionary<string, object> data = snapshot.ToDictionary();
                    SetDataFromDictionary(data);
                    HasProfile = true;
                    Debug.Log("Đã tải xong dữ liệu cơ bản từ Firestore.");

                    // Tiếp tục tải CurrentLevel từ bảng riêng
                    db.Collection("CurrentLevel").Document(uid).GetSnapshotAsync().ContinueWithOnMainThread(levelTask => {
                        if (!levelTask.IsFaulted && levelTask.Result.Exists)
                        {
                            var levelData = levelTask.Result.ToDictionary();
                            CurrentLevel = levelData.ContainsKey("level") ? Convert.ToInt32(levelData["level"]) : 1;
                            Debug.Log("Đã tải xong Level từ bảng CurrentLevel: " + CurrentLevel);
                        }
                        onComplete?.Invoke();
                    });
                }
                else
                {
                    HasProfile = false;
                    Debug.Log("Không tìm thấy dữ liệu người chơi trên Firestore (người dùng mới).");
                    onComplete?.Invoke();
                }
            }
        });
    }

    private void SetDataFromDictionary(Dictionary<string, object> data)
    {
        Username = data.ContainsKey("username") ? data["username"].ToString() : "";
        Attack = data.ContainsKey("attack") ? Convert.ToSingle(data["attack"]) : 20f;
        MaxHp = data.ContainsKey("maxHp") ? Convert.ToSingle(data["maxHp"]) : 100f;
        Speed = data.ContainsKey("speed") ? Convert.ToSingle(data["speed"]) : 10f;
        Defend = data.ContainsKey("defend") ? Convert.ToSingle(data["defend"]) : 5f;
        Gold = data.ContainsKey("gold") ? Convert.ToInt32(data["gold"]) : 0;
        // CurrentLevel giờ được đọc riêng từ bảng CurrentLevel, không đọc từ Users nữa
        if (data.ContainsKey("currentLevel")) CurrentLevel = Convert.ToInt32(data["currentLevel"]); // Giữ lại backup nếu bảng mới chưa có
    }

    public void UpdateProfileData(string username, float attack, float maxHp, float speed, float defend, int gold, int currentLevel)
    {
        HasProfile = true;
        Username = username;
        Attack = attack;
        MaxHp = maxHp;
        Speed = speed;
        Defend = defend;
        Gold = gold;
        CurrentLevel = currentLevel;
    }

    public void UnlockLevel(int newLevel)
    {
        if (newLevel > CurrentLevel)
        {
            CurrentLevel = newLevel;
            
            FirebaseAuth auth = FirebaseAuth.DefaultInstance;
            if (auth != null && auth.CurrentUser != null)
            {
                FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
                string uid = auth.CurrentUser.UserId;
                
                // Cập nhật ở bảng CurrentLevel
                Dictionary<string, object> levelUpdates = new Dictionary<string, object>
                {
                    { "level", CurrentLevel }
                };
                db.Collection("CurrentLevel").Document(uid).SetAsync(levelUpdates, SetOptions.MergeAll).ContinueWithOnMainThread(task => {
                    if (task.IsFaulted) Debug.LogError("Lỗi khi lưu cấp độ vào CurrentLevel: " + task.Exception);
                    else Debug.Log("Đã mở khóa Level " + CurrentLevel + " thành công trên Collection CurrentLevel.");
                });
            }
        }
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        if (auth != null && auth.CurrentUser != null)
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            string uid = auth.CurrentUser.UserId;
            
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "gold", Gold }
            };
            db.Collection("Users").Document(uid).SetAsync(updates, SetOptions.MergeAll).ContinueWithOnMainThread(task => {
                if (task.IsFaulted) Debug.LogError("Lỗi khi lưu vàng: " + task.Exception);
                else Debug.Log($"Đã cộng {amount} vàng thành công! Tổng vàng: {Gold}");
            });
        }
    }
}
