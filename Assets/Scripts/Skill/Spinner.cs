using System.Collections.Generic;
using UnityEngine;

public class Spinner : MonoBehaviour
{
    [Header("Combat Settings")]
    [Tooltip("Hệ số nhân sát thương từ tấn công gốc của Player")]
    public float damageMultiplier = 1f;
    [Tooltip("Thời gian chờ giữa 2 lần chém lên CÙNG 1 kẻ địch (giây). Giúp quái không bị trừ máu liên tục trong 1 giây")]
    public float damageCooldown = 0.5f;
    [Tooltip("Lực đẩy lùi quái vật khi bị chém trúng")]
    public float knockbackForce = 8f;

    [Header("Visual Settings")]
    [Tooltip("Tốc độ tự xoay quanh trục của con quay (Để 0 nếu bạn đã dùng Animation)")]
    public float rotationSpeed = 360f;

    private Soldier playerSoldier;
    
    // Lưu lại thời điểm chém trúng từng con quái để tính Cooldown
    private Dictionary<Collider2D, float> lastDamageTime = new Dictionary<Collider2D, float>();

    void Start()
    {
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            playerSoldier = pObj.GetComponent<Soldier>();
        }
    }

    void Update()
    {
        // Hiệu ứng tự xoay (Nếu có)
        if (rotationSpeed != 0)
        {
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }

        CleanupDictionary();
    }

    // Dùng OnTriggerStay2D để gây sát thương liên tục khi quái vẫn đứng trong con quay
    private void OnTriggerStay2D(Collider2D col)
    {
        if (col.CompareTag("Enemy"))
        {
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null && enemy.hp > 0)
            {
                // Nếu quái chưa từng bị chém, hoặc đã qua thời gian Cooldown
                if (!lastDamageTime.ContainsKey(col) || Time.time - lastDamageTime[col] >= damageCooldown)
                {
                    // Lấy sát thương người chơi x Hệ số
                    float damage = playerSoldier != null ? playerSoldier.attack * damageMultiplier : 10f * damageMultiplier;
                    
                    enemy.TakeDame(damage);

                    // Đẩy lùi quái ra xa khỏi tâm con quay
                    Vector2 pushDir = (col.transform.position - transform.position).normalized;
                    enemy.ApplyKnockback(pushDir * knockbackForce);
                    
                    // Cập nhật lại mốc thời gian vừa bị chém
                    lastDamageTime[col] = Time.time;
                }
            }
        }
    }

    // Dọn dẹp bộ nhớ: Xoá thông tin của những con quái đã bị tiêu diệt
    private void CleanupDictionary()
    {
        if (Time.frameCount % 60 == 0) // Mỗi 60 frame quét 1 lần cho nhẹ máy
        {
            List<Collider2D> keysToRemove = new List<Collider2D>();
            foreach (var key in lastDamageTime.Keys)
            {
                if (key == null) keysToRemove.Add(key);
            }
            foreach (var key in keysToRemove)
            {
                lastDamageTime.Remove(key);
            }
        }
    }
}
