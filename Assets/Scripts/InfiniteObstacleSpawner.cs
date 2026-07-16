using UnityEngine;
using System.Collections.Generic;

public class InfiniteObstacleSpawner : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [Tooltip("Danh sách các Prefab vật cản (vd: Hòn đá, Bức tường, Thùng phuy...)")]
    [SerializeField] private GameObject[] obstaclePrefabs;
    
    [Tooltip("Số lượng vật cản tối đa được phép tồn tại cùng lúc trên bản đồ")]
    [SerializeField] private int maxObstacles = 30;

    [Tooltip("LayerMask chứa các vật thể không được phép đẻ đè lên (vd: Obstacle, Enemy, Player...)")]
    [SerializeField] private LayerMask checkMask;
    
    [Tooltip("Bán kính an toàn xung quanh vật cản. Càng to thì các vật cản đẻ càng cách xa nhau")]
    [SerializeField] private float safeRadius = 20f;

    [Header("Spawn Radius")]
    [Tooltip("Bán kính tối thiểu để spawn (Nên để lớn hơn màn hình để vật cản không đẻ ngay giữa mặt người chơi)")]
    [SerializeField] private float minSpawnRadius = 15f;
    
    [Tooltip("Bán kính tối đa để đẻ vật cản")]
    [SerializeField] private float maxSpawnRadius = 20f;

    [Tooltip("Khoảng cách tối đa so với Camera. Nếu vật cản đi xa hơn mức này sẽ bị xóa để nhẹ máy")]
    [SerializeField] private float destroyRadius = 25f;

    [Header("Time")]
    [Tooltip("Bao lâu thì kiểm tra đẻ vật cản 1 lần (giây)")]
    [SerializeField] private float spawnInterval = 0.5f;

    private Transform cam;
    private List<GameObject> activeObstacles = new List<GameObject>();
    private float spawnTimer;

    private void Start()
    {
        if (Camera.main != null)
        {
            cam = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("InfiniteObstacleSpawner: Không tìm thấy Camera.main!");
        }
    }

    private void Update()
    {
        if (cam == null || obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        // 1. Quét và dọn dẹp các vật cản đã đi quá xa Camera (tức là người chơi đã bỏ lại chúng phía sau)
        CleanupFarObstacles();

        // 2. Spawn vật cản mới nếu trên bản đồ đang có ít hơn giới hạn maxObstacles
        if (activeObstacles.Count < maxObstacles)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                SpawnObstacle();
                spawnTimer = 0f;
            }
        }
    }

    private void SpawnObstacle()
    {
        // Chọn ngẫu nhiên 1 loại vật cản trong danh sách
        GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];

        Vector3 spawnPos = Vector3.zero;
        bool foundSafePosition = false;

        // Thử tìm vị trí ngẫu nhiên tối đa 10 lần, nếu chỗ nào cũng bị kẹt thì thôi bỏ qua chờ lần đẻ sau
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized; 
            float randomDistance = Random.Range(minSpawnRadius, maxSpawnRadius);
            spawnPos = cam.position + new Vector3(randomDirection.x, randomDirection.y, 0) * randomDistance;
            spawnPos.z = 0f;

            // Quét hình tròn xem vị trí này có bị vướng hòn đá cũ hay con quái nào không
            Collider2D hit = Physics2D.OverlapCircle(spawnPos, safeRadius, checkMask);
            
            // Nếu hit == null nghĩa là vùng này hoàn toàn trống trải
            if (hit == null)
            {
                foundSafePosition = true;
                break; // Chốt vị trí này! Thoát vòng lặp
            }
        }

        // Nếu thử 10 lần xui quá vẫn đụng vật cản cũ thì hủy kèo đẻ, chờ lượt sau
        if (!foundSafePosition) return;

        Quaternion randomRotation = Quaternion.Euler(0f, 0f, Random.Range(-60f, 60f));

        // Tạo ra vật cản tại tọa độ đã tính
        GameObject obs = Instantiate(prefab, spawnPos, randomRotation);
        
        // Gôm nó vào làm con của Object này cho Hierarchy gọn gàng
        obs.transform.SetParent(transform); 
        
        // Thêm vào danh sách để quản lý
        activeObstacles.Add(obs);
    }

    private void CleanupFarObstacles()
    {
        // Quét ngược mảng từ cuối lên đầu (cách an toàn nhất khi muốn xóa phần tử trong List)
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            GameObject obs = activeObstacles[i];
            
            // Nếu vật cản đã bị hủy vì lý do nào đó thì bỏ qua
            if (obs == null)
            {
                activeObstacles.RemoveAt(i);
                continue;
            }

            // Đo khoảng cách từ Camera tới vật cản
            float distance = Vector2.Distance(cam.position, obs.transform.position);
            
            // Nếu vật cản nằm ngoài bán kính tiêu hủy -> Xóa nó đi
            if (distance > destroyRadius)
            {
                activeObstacles.RemoveAt(i);
                Destroy(obs);
            }
        }
    }
}
