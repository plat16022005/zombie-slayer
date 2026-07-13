using UnityEngine;

public enum ScenarioWinCondition
{
    KillAllZombies, // Tiêu diệt hết toàn bộ quái trên map sau đợt cuối
    SurviveTime,    // Sống sót đủ thời gian quy định
    KillBoss        // Tiêu diệt con boss (theo Prefab chỉ định)
}

[System.Serializable]
public class ZombieGroup
{
    [Tooltip("Loại Zombie sẽ sinh ra trong nhóm này")]
    public GameObject enemyPrefab;
    [Tooltip("Số lượng cần sinh ra")]
    public int count = 5;
    [Tooltip("Khoảng cách thời gian sinh ra giữa từng con")]
    public float spawnInterval = 0.5f;
}

[System.Serializable]
public class ZombieWave
{
    public string waveName = "Wave 1";
    [Tooltip("Chờ bao lâu mới bắt đầu đợt này (tính từ lúc đợt trước hoàn thành)")]
    public float delayBeforeWave = 5f;
    [Tooltip("Các nhóm Zombie sẽ cùng lúc sinh ra trong đợt này")]
    public ZombieGroup[] groups;
}

[CreateAssetMenu(fileName = "New Zombie Scenario", menuName = "Zombie Slayer/Zombie Scenario")]
public class ZombieScenario : ScriptableObject
{
    [Header("Win Condition Settings")]
    [Tooltip("Điều kiện để chiến thắng kịch bản này")]
    public ScenarioWinCondition winCondition = ScenarioWinCondition.KillAllZombies;
    
    [Tooltip("Thời gian sống sót (giây) - Chỉ dùng nếu WinCondition = SurviveTime")]
    public float surviveTimeInSeconds = 120f;
    
    [Tooltip("Prefab Boss - Chỉ dùng nếu WinCondition = KillBoss")]
    public GameObject bossPrefab;

    [Header("Wave Setup")]
    public ZombieWave[] waves;
}
