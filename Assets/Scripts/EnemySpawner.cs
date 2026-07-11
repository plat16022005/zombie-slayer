using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private GameObject[] enemyPrefabs; // Danh sách các loại quái sẽ sinh ra
    [SerializeField] private float spawnInterval = 3f;  // Thời gian sinh quái (3 giây)
    [SerializeField] private float spawnMargin = 0.1f;  // Khoảng cách nằm ngoài rìa màn hình (0.1 = 10%)

    private float spawnTimer;

    private void Start()
    {
        spawnTimer = spawnInterval;
    }

    private void Update()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnEnemy();
            spawnTimer = spawnInterval;
        }
    }

    private void SpawnEnemy()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Viewport toạ độ: (0,0) là góc dưới trái, (1,1) là góc trên phải.
        // Để sinh quái ngoài viền, ta dùng toạ độ nhỏ hơn 0 hoặc lớn hơn 1.
        float min = 0f - spawnMargin;
        float max = 1f + spawnMargin;

        Vector2 viewportSpawnPos = Vector2.zero;

        // Chọn ngẫu nhiên 1 trong 4 cạnh màn hình: 0=Trái, 1=Phải, 2=Dưới, 3=Trên
        int randomEdge = Random.Range(0, 4);
        switch (randomEdge)
        {
            case 0: // Trái
                viewportSpawnPos = new Vector2(min, Random.Range(min, max));
                break;
            case 1: // Phải
                viewportSpawnPos = new Vector2(max, Random.Range(min, max));
                break;
            case 2: // Dưới
                viewportSpawnPos = new Vector2(Random.Range(min, max), min);
                break;
            case 3: // Trên
                viewportSpawnPos = new Vector2(Random.Range(min, max), max);
                break;
        }

        // Chuyển toạ độ Viewport ảo thành toạ độ thực trong Game World
        Vector3 worldSpawnPos = cam.ViewportToWorldPoint(new Vector3(viewportSpawnPos.x, viewportSpawnPos.y, Mathf.Abs(cam.transform.position.z)));
        worldSpawnPos.z = 0f; // Đảm bảo ở đúng trục Z 2D

        // Chọn ngẫu nhiên 1 loại quái
        GameObject randomEnemy = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        // Sinh ra quái vật
        Instantiate(randomEnemy, worldSpawnPos, Quaternion.identity);
    }
}
