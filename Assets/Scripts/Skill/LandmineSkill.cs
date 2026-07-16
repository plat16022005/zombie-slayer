using UnityEngine;

public class LandmineSkill : MonoBehaviour
{
    [Header("Skill Settings")]
    [Tooltip("Kéo Prefab Quả Mìn (đã gắn script Landmine) vào đây")]
    public GameObject landminePrefab;
    [Tooltip("Thời gian ném 1 quả mìn (giây)")]
    public float spawnCooldown = 3f;
    [Tooltip("Khoảng cách ném mìn văng ra xa người chơi tối đa bao nhiêu")]
    public float tossRadius = 4f;
    [Tooltip("Số lượng mìn ném ra trong một đợt")]
    public int minesPerSpawn = 6;

    [Header("Level Settings")]
    public int currentLevel = 1;
    public int maxLevel = 5;

    // Các chỉ số nâng cấp sẽ được truyền cho quả mìn
    private float currentDamageMultiplier = 1f;
    private float currentExplosionRadius = 1.5f;

    private Transform player;
    private float timer;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        timer = 0f;
        UpdateStatsForLevel();
    }

    void Update()
    {
        if (player == null || landminePrefab == null) return;

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            for (int i = 0; i < minesPerSpawn; i++)
            {
                TossMine();
            }
            timer = spawnCooldown;
        }
    }

    private void TossMine()
    {
        // Random.insideUnitCircle trả về 1 điểm ngẫu nhiên trong bán kính 1
        // Nhân với tossRadius để mở rộng tầm ném
        Vector2 randomOffset = Random.insideUnitCircle * tossRadius;
        
        // Vị trí mìn rơi xuống đất
        Vector3 spawnPos = player.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

        GameObject mine = Instantiate(landminePrefab, spawnPos, Quaternion.identity);
        
        // Truyền sức mạnh của Level hiện tại vào quả mìn
        Landmine script = mine.GetComponent<Landmine>();
        if (script != null)
        {
            script.InitStats(currentDamageMultiplier, currentExplosionRadius);
        }
    }

    public void LevelUp()
    {
        if (currentLevel >= maxLevel) return;
        currentLevel++;
        UpdateStatsForLevel();
    }

    private void UpdateStatsForLevel()
    {
        spawnCooldown -= 0.5f;
        minesPerSpawn += 1;
    }
}
