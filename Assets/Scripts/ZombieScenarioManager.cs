using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieScenarioManager : MonoBehaviour
{
    [Header("Scenario Settings")]
    [Tooltip("Kéo thả file Scenario (Scriptable Object) vào đây")]
    public ZombieScenario currentScenario;
    
    [Header("Spawn Settings")]
    [Tooltip("Khoảng cách nằm ngoài rìa màn hình (0.1 = 10%)")]
    public float spawnMargin = 0.1f;

    private int currentWaveIndex = 0;
    private bool hasSpawnedAllWaves = false;
    
    // Tracking Win Conditions
    private bool isGameWon = false;
    private float survivalTimer = 0f;
    private bool isBossSpawned = false;
    private GameObject activeBoss;
    
    // Boss Info Cache
    private string cachedBossName = "";
    private Sprite cachedBossAvatar = null;

    private void Start()
    {
        if (currentScenario != null && currentScenario.waves != null && currentScenario.waves.Length > 0)
        {
            StartCoroutine(PlayScenarioCoroutine());

            // Hiển thị mục tiêu màn chơi
            string objective = "";
            switch (currentScenario.winCondition)
            {
                case ScenarioWinCondition.SurviveTime:
                    objective = $"Sống sót trong {currentScenario.surviveTimeInSeconds} giây!";
                    break;
                case ScenarioWinCondition.KillAllZombies:
                    objective = "Tiêu diệt toàn bộ thây ma!";
                    break;
                case ScenarioWinCondition.KillBoss:
                    objective = "Sống sót và tiêu diệt Boss!";
                    break;
            }
            if (PlayerUIManager.Instance != null && !string.IsNullOrEmpty(objective))
            {
                PlayerUIManager.Instance.ShowLevelObjective($"MỤC TIÊU: {objective}");
            }
        }
        else
        {
            Debug.LogWarning("Chưa gắn ZombieScenario hoặc kịch bản trống!");
        }
    }

    private void Update()
    {
        if (isGameWon || currentScenario == null) return;
        CheckWinCondition();
        UpdateProgressUI();
    }

    private void UpdateProgressUI()
    {
        if (PlayerUIManager.Instance == null) return;

        switch (currentScenario.winCondition)
        {
            case ScenarioWinCondition.SurviveTime:
                float timeLeft = Mathf.Max(0, currentScenario.surviveTimeInSeconds - survivalTimer);
                PlayerUIManager.Instance.UpdateLevelProgress($"Thời gian: {Mathf.CeilToInt(timeLeft)}s");
                break;

            case ScenarioWinCondition.KillAllZombies:
                if (currentScenario.waves != null && currentScenario.waves.Length > 0)
                {
                    int totalWaves = currentScenario.waves.Length;
                    if (hasSpawnedAllWaves)
                    {
                        PlayerUIManager.Instance.UpdateLevelProgress($"Tiêu diệt toàn bộ Zombie!");
                    }
                    else
                    {
                        PlayerUIManager.Instance.UpdateLevelProgress($"Wave: {currentWaveIndex + 1}/{totalWaves}");
                    }
                }
                break;

            case ScenarioWinCondition.KillBoss:
                if (isBossSpawned && activeBoss != null)
                {
                    PlayerUIManager.Instance.UpdateLevelProgress($"Tiêu diệt Boss!");
                    Enemy bossScript = activeBoss.GetComponent<Enemy>();
                    if (bossScript != null)
                    {
                        PlayerUIManager.Instance.UpdateBossHealth(bossScript.hp, bossScript.maxHp, cachedBossName, cachedBossAvatar);
                    }
                }
                else if (!isBossSpawned)
                {
                    PlayerUIManager.Instance.UpdateLevelProgress($"Sống đến khi Boss xuất hiện!");
                }
                break;
        }
    }

    private void CheckWinCondition()
    {
        switch (currentScenario.winCondition)
        {
            case ScenarioWinCondition.SurviveTime:
                survivalTimer += Time.deltaTime;
                if (survivalTimer >= currentScenario.surviveTimeInSeconds)
                {
                    WinGame("Sống sót thành công qua thời gian!");
                }
                break;

            case ScenarioWinCondition.KillAllZombies:
                // Nếu đã gọi ra đợt cuối và không còn quái vật nào có Tag "Enemy" trên bản đồ
                if (hasSpawnedAllWaves)
                {
                    GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
                    if (enemies.Length == 0)
                    {
                        WinGame("Đã quét sạch toàn bộ Zombie!");
                    }
                }
                break;

            case ScenarioWinCondition.KillBoss:
                // Nếu boss đã được sinh ra nhưng bây giờ là null (bị giết và destroy)
                if (isBossSpawned && activeBoss == null)
                {
                    WinGame("Đã tiêu diệt được Boss!");
                }
                break;
        }
    }

    private void WinGame(string reason)
    {
        isGameWon = true;
        Debug.Log($"<color=green>[VICTORY] CHIẾN THẮNG: {reason}</color>");
        
        int earnedGold = 0;
        
        // Mở khóa màn chơi tiếp theo và tặng vàng
        if (DataGame.Instance != null && DataGame.Instance.HasProfile)
        {
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentSceneName.StartsWith("Level"))
            {
                if (int.TryParse(currentSceneName.Replace("Level", ""), out int currentPlayingLevel))
                {
                    // Nếu màn chơi hiện hành là màn chơi cao nhất được mở, ta mở màn tiếp theo và tặng vàng (chỉ nhận vàng lần đầu)
                    if (currentPlayingLevel == DataGame.Instance.CurrentLevel)
                    {
                        DataGame.Instance.UnlockLevel(currentPlayingLevel + 1);
                        
                        earnedGold = 1000;
                        DataGame.Instance.AddGold(earnedGold);
                    }
                }
            }
        }

        // Gọi UI hiển thị màn hình Win, số vàng nhận được và dừng thời gian
        if (PlayerUIManager.Instance != null)
        {
            PlayerUIManager.Instance.ShowVictoryPanel(reason, earnedGold);
            PlayerUIManager.Instance.HideBossHealth(); // Ẩn thanh máu Boss khi thắng
        }
    }

    private IEnumerator PlayScenarioCoroutine()
    {
        for (int i = 0; i < currentScenario.waves.Length; i++)
        {
            currentWaveIndex = i;
            ZombieWave currentWave = currentScenario.waves[i];
            
            Debug.Log($"[Scenario] Đang chờ {currentWave.delayBeforeWave}s để bắt đầu: {currentWave.waveName}");
            yield return new WaitForSeconds(currentWave.delayBeforeWave);

            Debug.Log($"[Scenario] BẮT ĐẦU: {currentWave.waveName}");

            List<Coroutine> groupCoroutines = new List<Coroutine>();
            foreach (ZombieGroup group in currentWave.groups)
            {
                groupCoroutines.Add(StartCoroutine(SpawnGroupCoroutine(group)));
            }

            foreach (Coroutine c in groupCoroutines)
            {
                yield return c;
            }
            
            Debug.Log($"[Scenario] {currentWave.waveName} đã sinh xong toàn bộ quái.");
        }

        Debug.Log("[Scenario] HOÀN TẤT SINH QUÁI CỦA KỊCH BẢN!");
        hasSpawnedAllWaves = true;
    }

    private IEnumerator SpawnGroupCoroutine(ZombieGroup group)
    {
        if (group.enemyPrefab == null) yield break;

        for (int i = 0; i < group.count; i++)
        {
            SpawnSingleZombie(group.enemyPrefab);
            yield return new WaitForSeconds(group.spawnInterval);
        }
    }

    private void SpawnSingleZombie(GameObject prefab)
    {
        Vector3 spawnPos = GetRandomSpawnPositionOutsideScreen();
        GameObject zombie = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Đánh tag cho zombie nếu prefab chưa có, để dễ kiểm tra điều kiện thắng KillAll
        if (zombie.tag != "Enemy") zombie.tag = "Enemy";

        // Theo dõi Boss nếu chế độ là KillBoss
        if (currentScenario.winCondition == ScenarioWinCondition.KillBoss && prefab == currentScenario.bossPrefab)
        {
            activeBoss = zombie;
            isBossSpawned = true;
            
            // Lấy tên từ Prefab
            cachedBossName = prefab.name;
            
            // Lấy Avatar từ GameObject con tên "Head" (kể cả nằm sâu bên trong)
            SpriteRenderer[] allSprites = zombie.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer sr in allSprites)
            {
                if (sr.gameObject.name == "Head")
                {
                    cachedBossAvatar = sr.sprite;
                    break;
                }
            }
        }
    }

    private Vector3 GetRandomSpawnPositionOutsideScreen()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        float min = 0f - spawnMargin;
        float max = 1f + spawnMargin;

        Vector2 viewportSpawnPos = Vector2.zero;

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

        Vector3 worldSpawnPos = cam.ViewportToWorldPoint(new Vector3(viewportSpawnPos.x, viewportSpawnPos.y, Mathf.Abs(cam.transform.position.z)));
        worldSpawnPos.z = 0f; 
        return worldSpawnPos;
    }
}
