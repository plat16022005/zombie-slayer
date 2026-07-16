using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpSpawner : MonoBehaviour
{
    [Header("Cấu hình Spawner")]
    [Tooltip("Kéo Exp Database vào đây để lấy các loại ngọc")]
    public ExpDatabase expDatabase;

    [Tooltip("Khoảng thời gian (giây) giữa 2 lần sinh ngọc")]
    public float spawnInterval = 3f;

    [Tooltip("Bán kính tối thiểu sinh ngọc (so với Player)")]
    public float minSpawnRadius = 5f;

    [Tooltip("Bán kính tối đa sinh ngọc (so với Player)")]
    public float maxSpawnRadius = 15f;

    [Tooltip("Số lượng ngọc tối đa cho phép sinh ra cùng lúc (để tránh lag)")]
    public int maxGemsOnMap = 50;

    [Tooltip("Lượng kinh nghiệm ngẫu nhiên rớt ra mỗi lần")]
    public int minExpDrop = 10;
    public int maxExpDrop = 30;

    private Transform player;
    private float spawnTimer;

    void Start()
    {
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player = pObj.transform;
        }

        spawnTimer = spawnInterval;
    }

    void Update()
    {
        if (player == null || expDatabase == null) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnRandomExp();
            spawnTimer = spawnInterval;
        }
    }

    private void SpawnRandomExp()
    {
        // Kiểm tra xem hiện tại trên bản đồ có bao nhiêu ngọc
        ExpGem[] existingGems = FindObjectsOfType<ExpGem>();
        if (existingGems.Length >= maxGemsOnMap) return;

        // Tìm một vị trí ngẫu nhiên xung quanh player
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(minSpawnRadius, maxSpawnRadius);
        Vector3 spawnPosition = player.position + new Vector3(randomDirection.x * randomDistance, randomDirection.y * randomDistance, 0f);

        // Random lượng exp
        int randomExp = Random.Range(minExpDrop, maxExpDrop + 1);

        // Sinh ngọc
        expDatabase.SpawnExpGems(randomExp, spawnPosition);
        
        // Debug.Log($"Đã sinh ngọc ({randomExp} exp) tại {spawnPosition}");
    }
}
