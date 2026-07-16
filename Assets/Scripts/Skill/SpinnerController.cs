using System.Collections.Generic;
using UnityEngine;

public class SpinnerController : MonoBehaviour
{
    [Header("Skill Settings")]
    [Tooltip("Prefab của con quay (đã gắn sẵn script Spinner)")]
    public GameObject spinnerPrefab;
    [Tooltip("Khoảng cách (bán kính) từ người chơi đến con quay")]
    public float orbitRadius = 2.5f;
    [Tooltip("Tốc độ con quay lượn vòng quanh người chơi (độ/giây)")]
    public float orbitSpeed = 180f;

    [Header("Cooldown Settings")]
    [Tooltip("Thời gian con quay xuất hiện chém quái (giây)")]
    public float activeDuration = 5f;
    [Tooltip("Thời gian con quay biến mất để hồi chiêu (giây)")]
    public float cooldownDuration = 5f;

    [Header("Level Settings")]
    public int currentLevel = 1;
    public int maxLevel = 5;

    private Transform player;
    private List<GameObject> activeSpinners = new List<GameObject>();
    private float currentAngle = 0f;
    
    // Cooldown state
    private bool isSpinningActive = true;
    private float stateTimer = 0f;

    void Start()
    {
        // Tự động tìm Player
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player = pObj.transform;
        }

        // Khởi tạo số lượng con quay theo cấp độ hiện tại
        SpawnSpinnersForCurrentLevel();
    }

    void Update()
    {
        if (player == null || activeSpinners.Count == 0) return;

        stateTimer += Time.deltaTime;

        if (isSpinningActive)
        {
            // Nếu đã hết thời gian hiển thị -> Tắt đi để hồi chiêu
            if (stateTimer >= activeDuration)
            {
                isSpinningActive = false;
                stateTimer = 0f;
                SetSpinnersActive(false);
            }
            else
            {
                // Đang trong thời gian hiển thị -> Tiếp tục xoay quanh người chơi
                currentAngle += orbitSpeed * Time.deltaTime;
                if (currentAngle >= 360f) currentAngle -= 360f;

                float angleStep = 360f / activeSpinners.Count;

                for (int i = 0; i < activeSpinners.Count; i++)
                {
                    if (activeSpinners[i] == null) continue;

                    float spinnerAngle = currentAngle + (i * angleStep);
                    float rad = spinnerAngle * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;
                    activeSpinners[i].transform.position = player.position + offset;
                }
            }
        }
        else
        {
            // Nếu đang trong thời gian hồi chiêu -> Chờ hết giờ thì bật lại
            if (stateTimer >= cooldownDuration)
            {
                isSpinningActive = true;
                stateTimer = 0f;
                SetSpinnersActive(true);
            }
        }
    }

    private void SetSpinnersActive(bool active)
    {
        foreach (var spinner in activeSpinners)
        {
            if (spinner != null)
                spinner.SetActive(active);
        }
    }

    public void LevelUp()
    {
        if (currentLevel >= maxLevel) return;
        currentLevel++;
        
        // Tự động sinh thêm con quay
        SpawnSpinnersForCurrentLevel();

        // Mỗi level tăng thêm tốc độ quay
        orbitSpeed += 30f;
        cooldownDuration -= 1f;
    }

    private void SpawnSpinnersForCurrentLevel()
    {
        if (spinnerPrefab == null) return;

        // Công thức: Level 1 có 2 con, Level 2 có 3 con... Level 5 có 6 con
        int targetCount = currentLevel + 1;

        // Sinh thêm cho đủ số lượng
        while (activeSpinners.Count < targetCount)
        {
            GameObject newSpinner = Instantiate(spinnerPrefab, transform.position, Quaternion.identity);
            
            // (Tuỳ chọn) Gôm vào làm con của Controller này cho gọn Hierarchy
            newSpinner.transform.SetParent(transform);
            
            // Cập nhật trạng thái hiển thị khớp với trạng thái hiện tại của bộ đếm
            newSpinner.SetActive(isSpinningActive);

            activeSpinners.Add(newSpinner);
        }
    }
}
