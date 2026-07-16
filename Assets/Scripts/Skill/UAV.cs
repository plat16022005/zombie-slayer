using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UAV : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("Khoảng cách bay lơ lửng so với người chơi")]
    public Vector3 offset = new Vector3(-1.5f, 2f, 0f);
    [Tooltip("Tốc độ bay theo người chơi")]
    public float followSpeed = 5f;

    [Header("Combat Settings")]
    [Tooltip("Bán kính tìm quái")]
    public float attackRadius = 15f;
    [Tooltip("Bao lâu thì xả đạn 1 lần (giây)")]
    public float fireInterval = 5f;
    [Tooltip("Số viên đạn xả ra mỗi lần")]
    public int bulletsPerBurst = 10;
    [Tooltip("Độ trễ giữa từng viên đạn trong 1 lần xả")]
    public float timeBetweenBullets = 0.1f;
    
    [Tooltip("Prefab viên đạn UAV sẽ bắn ra")]
    public GameObject bulletPrefab;
    [Tooltip("Vị trí nòng súng của UAV (Nếu để trống sẽ bắn từ giữa UAV)")]
    public Transform firePoint;

    [Header("Level Settings")]
    [Tooltip("Cấp độ hiện tại của UAV")]
    public int currentLevel = 1;
    public int maxLevel = 5;
    private float damageMultiplier = 1f;

    private Transform player;
    private Soldier playerSoldier;
    private float fireTimer;

    public void LevelUp()
    {
        if (currentLevel >= maxLevel) return;
        currentLevel++;

        // Logic nâng cấp chỉ số theo cấp độ
        switch (currentLevel)
        {
            case 2:
                bulletsPerBurst += 5; // Lv2: Bắn nhiều đạn hơn mỗi lần xả
                break;
            case 3:
                fireInterval = Mathf.Max(1f, fireInterval - 1f); // Lv3: Xả đạn nhanh hơn
                damageMultiplier = 1.2f; // Tăng 20% sát thương
                break;
            case 4:
                bulletsPerBurst += 5; 
                attackRadius += 5f; // Lv4: Tầm phát hiện quái xa hơn
                break;
            case 5:
                fireInterval = Mathf.Max(1f, fireInterval - 1f); 
                damageMultiplier = 1.5f; // Lv5: Tăng 50% sát thương
                break;
        }
    }

    void Start()
    {
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player = pObj.transform;
            playerSoldier = pObj.GetComponent<Soldier>();
        }

        fireTimer = fireInterval;
    }

    void Update()
    {
        if (player == null) return;

        // Tính vị trí cần bay tới (giữ khoảng cách offset cơ bản)
        Vector3 targetPos = player.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);

        // Đếm ngược thời gian xả đạn
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0)
        {
            StartCoroutine(FireBurstRoutine());
            fireTimer = fireInterval;
        }
    }

    private IEnumerator FireBurstRoutine()
    {
        for (int i = 0; i < bulletsPerBurst; i++)
        {
            Transform target = FindRandomEnemy();
            if (target != null && bulletPrefab != null)
            {
                Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
                GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
                Bullet bullet = bulletObj.GetComponent<Bullet>();
                
                if (bullet != null)
                {
                    // Hướng bay từ nòng súng UAV tới quái
                    Vector2 direction = (target.position - spawnPos).normalized;
                    
                    // Sát thương bằng sát thương của Soldier hiện tại nhân với hệ số cấp độ
                    float baseDamage = playerSoldier != null ? playerSoldier.attack : 10f;
                    float finalDamage = baseDamage * damageMultiplier;
                    
                    bullet.Init(direction, finalDamage);
                }
            }
            // Chờ một chút rồi mới bắn viên tiếp theo
            yield return new WaitForSeconds(timeBetweenBullets);
        }
    }

    private Transform FindRandomEnemy()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, attackRadius);
        List<Transform> enemies = new List<Transform>();

        foreach (var col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                Enemy enemyScript = col.GetComponent<Enemy>();
                // Chỉ nhắm vào những con còn sống
                if (enemyScript != null && enemyScript.hp > 0)
                {
                    enemies.Add(col.transform);
                }
            }
        }

        if (enemies.Count > 0)
        {
            return enemies[Random.Range(0, enemies.Count)];
        }
        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
