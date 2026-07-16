using UnityEngine;

public class Landmine : MonoBehaviour
{
    [Header("Explosion Settings")]
    [Tooltip("Bán kính vụ nổ AoE")]
    public float explosionRadius = 1.5f;
    [Tooltip("Hệ số sát thương nhân với tấn công của Player")]
    public float damageMultiplier = 1f;
    [Tooltip("Lực hất văng quái ra khỏi tâm nổ")]
    public float knockbackForce = 3f;

    [Header("VFX Settings")]
    [Tooltip("Prefab hiệu ứng vụ nổ (Máu, Lửa, Bụi...)")]
    public GameObject explosionVFX;

    private Soldier playerSoldier;

    void Start()
    {
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            playerSoldier = pObj.GetComponent<Soldier>();
        }
    }

    // Khi quái vật dẫm vào thì kích nổ
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        // 1. Sinh hiệu ứng nổ (Nếu có)
        if (explosionVFX != null)
        {
            Instantiate(explosionVFX, transform.position, Quaternion.identity);
        }

        // Tính sát thương cơ bản
        float finalDamage = playerSoldier != null ? playerSoldier.attack * damageMultiplier : 10f * damageMultiplier;

        // 2. Quét TẤT CẢ quái vật nằm trong bán kính nổ
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var col in hitEnemies)
        {
            if (col.CompareTag("Enemy"))
            {
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null && enemy.hp > 0)
                {
                    // Trừ máu
                    enemy.TakeDame(finalDamage);

                    // Đẩy lùi văng ra xa khỏi tâm quả mìn
                    Vector2 pushDir = (enemy.transform.position - transform.position).normalized;
                    enemy.ApplyKnockback(pushDir * knockbackForce);
                }
            }
        }

        // 3. Quả mìn tự huỷ sau khi nổ
        Destroy(gameObject);
    }

    // Nhận chỉ số sức mạnh từ LandmineSkill truyền qua
    public void InitStats(float multiplier, float radius)
    {
        damageMultiplier = multiplier;
        explosionRadius = radius;
        
        // Bạn có thể cho scale của quả mìn to ra theo bán kính nổ nếu thích
        // transform.localScale = new Vector3(radius, radius, 1f); 
    }

    // Vẽ vòng tròn đỏ trong Editor để dễ canh chỉnh
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
