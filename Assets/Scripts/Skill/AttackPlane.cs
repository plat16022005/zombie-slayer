using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackPlane : MonoBehaviour
{
    [Header("Flight Settings")]
    [Tooltip("Tốc độ bay của máy bay")]
    public float flySpeed = 10f;
    [Tooltip("Thời gian tự huỷ máy bay (tránh rác bộ nhớ nếu bay xuyên bản đồ)")]
    public float lifeTime = 10f;

    [Header("Combat Settings")]
    [Tooltip("Vị trí nòng súng máy trên thân máy bay")]
    public Transform firePoint;
    [Tooltip("Đạn súng máy để bắn")]
    public GameObject bulletPrefab;
    [Tooltip("Số lượng đạn xả ra trong 1 đợt bay")]
    public int bulletCount = 50;
    [Tooltip("Tốc độ xả đạn (Nhỏ = bắn càng nhanh)")]
    public float fireRate = 0.1f;
    [Tooltip("Tầm quét quái vật bên dưới mặt đất")]
    public float attackRadius = 20f;

    private Soldier playerSoldier;
    private float damageMultiplier = 1f;

    void Start()
    {
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            playerSoldier = pObj.GetComponent<Soldier>();
        }

        // Vừa bay ra là bắt đầu xả súng luôn
        StartCoroutine(FireBurstRoutine());

        // Hẹn giờ tự huỷ để xoá máy bay khi đã bay ra khỏi màn hình
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Máy bay luôn bay thẳng từ Phải qua Trái (bất chấp xoay hay không)
        transform.position += Vector3.left * flySpeed * Time.deltaTime;
    }

    private IEnumerator FireBurstRoutine()
    {
        // Đợi 0.5s cho máy bay bay vào giữa màn hình rồi mới xả đạn (tuỳ chọn)
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < bulletCount; i++)
        {
            Transform target = FindRandomEnemy();
            if (target != null && bulletPrefab != null)
            {
                // Lấy toạ độ nòng súng
                Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
                
                GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
                Bullet bullet = bulletObj.GetComponent<Bullet>();
                
                if (bullet != null)
                {
                    // Chĩa hướng từ nòng súng máy bay dội thẳng xuống quái
                    Vector2 direction = (target.position - spawnPos).normalized;
                    
                    // Tính sát thương
                    float baseDamage = playerSoldier != null ? playerSoldier.attack : 10f;
                    bullet.Init(direction, baseDamage * damageMultiplier);
                }
            }
            
            // Xả đạn liên thanh
            yield return new WaitForSeconds(fireRate);
        }
    }

    private Transform FindRandomEnemy()
    {
        // Quét tìm quái vật xung quanh vị trí của Người Chơi thay vì máy bay
        Vector3 searchCenter = playerSoldier != null ? playerSoldier.transform.position : transform.position;
        
        Collider2D[] colliders = Physics2D.OverlapCircleAll(searchCenter, attackRadius);
        List<Transform> enemies = new List<Transform>();

        foreach (var col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                Enemy enemyScript = col.GetComponent<Enemy>();
                if (enemyScript != null && enemyScript.hp > 0)
                {
                    enemies.Add(col.transform);
                }
            }
        }

        // Chọn ngẫu nhiên 1 con quái xấu số trong tầm ngắm
        if (enemies.Count > 0)
        {
            return enemies[Random.Range(0, enemies.Count)];
        }
        return null;
    }

    // Hàm này được gọi từ AirSupportSkill khi máy bay vừa sinh ra để bơm sức mạnh
    public void UpdateStats(int level)
    {
        switch (level)
        {
            case 1:
                bulletCount = 50;
                damageMultiplier = 1f;
                break;
            case 2:
                bulletCount = 70; // Xả nhiều đạn hơn
                break;
            case 3:
                bulletCount = 100;
                damageMultiplier = 1.2f; // Tăng sát thương
                break;
            case 4:
                bulletCount = 150;
                fireRate = 0.03f; // Đạn bay như mưa
                break;
            case 5:
                bulletCount = 200; // Mưa đạn càn quét
                damageMultiplier = 1.5f;
                break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = firePoint != null ? firePoint.position : transform.position;
        Gizmos.DrawWireSphere(center, attackRadius);
    }
}
