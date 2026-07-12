using System.Collections;
using UnityEngine;

/// <summary>
/// Lựu đạn: Bay về phía mục tiêu, đếm ngược rồi nổ gây sát thương AoE
/// </summary>
public class Boom : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float damage          = 100f; // Sát thương tối đa tâm nổ
    [SerializeField] private float explosionRadius = 3f;   // Bán kính nổ AoE
    [SerializeField] private float throwSpeed      = 10f;  // Tốc độ ném
    [SerializeField] private float heightScaleMax  = 0.5f; // Tỉ lệ to ra tối đa ở đỉnh parabol
    [SerializeField] private float arcHeight       = 2f;   // Độ cao của đường cong Parabol
    [SerializeField] private float knockbackForce  = 15f;  // Lực đẩy lùi enemy khi nổ
    [SerializeField] private float rotationSpeed   = 360f; // Độ xoay mỗi giây (degree/s)

    [Header("Optional VFX")]
    [SerializeField] private GameObject explosionVFXPrefab; // Prefab hiệu ứng nổ
    [SerializeField] private AudioClip  explosionSound;   // Âm thanh nổ
    private AudioSource audioSource;
    private Rigidbody2D rb;
    private bool hasExploded = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;  // Top-down 2D: không bị rơi
            // Không FreezeRotation — để boom tự xoay trong ThrowRoutine
        }
        else
            Debug.LogWarning("[Boom] Thiếu Rigidbody2D!");

        if (GetComponent<Collider2D>() == null)
            Debug.LogWarning("[Boom] Thiếu Collider2D (Is Trigger = true)!");
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // Chuyển sang 3D sound để nghe theo khoảng cách
            audioSource.rolloffMode = AudioRolloffMode.Linear; // Giảm âm thanh tuyến tính
            audioSource.minDistance = 5f;  // Dưới 5 units -> nghe to nhất (100%)
            audioSource.maxDistance = 25f; // Xa quá 25 units -> không nghe thấy gì
        }
    }

    /// <summary>
    /// Ném boom tới một vị trí đích cụ thể
    /// </summary>
    public void Throw(Vector2 targetPosition)
    {
        // Xoay hướng boom bay tới đích
        Vector2 dir = targetPosition - (Vector2)transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        StartCoroutine(ThrowRoutine(targetPosition));
    }

    private IEnumerator ThrowRoutine(Vector2 targetPos)
    {
        Vector2 startPos = transform.position;
        float distance = Vector2.Distance(startPos, targetPos);
        float duration = distance / throwSpeed;
        float time = 0f;

        Vector3 originalScale = transform.localScale;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            // 1. Di chuyển tuyến tính (cái bóng ở dưới đất)
            Vector2 linearPos = Vector2.Lerp(startPos, targetPos, t);

            // 2. Tính toán độ cao Parabol (từ 0 -> 1 -> 0)
            float heightParam = Mathf.Sin(t * Mathf.PI);

            // 3. Cập nhật vị trí thực: vị trí tuyến tính + nhô lên cao theo trục Y
            transform.position = linearPos + Vector2.up * (heightParam * arcHeight);

            // 4. Phóng to một chút để tạo cảm giác không gian 3D
            transform.localScale = originalScale * (1f + heightParam * heightScaleMax);

            // 5. Xoay boom trong khi bay
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

            yield return null;
        }

        transform.position = targetPos;
        transform.localScale = originalScale;
        
        // Nổ khi tới đích
        Explode();
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // Tìm tất cả object trong vùng nổ
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        int enemiesHit = 0;
        foreach (Collider2D hit in hits)
        {
            // Chỉ gây damage cho Enemy (theo tag), không hại Soldier
            if (!hit.CompareTag("Enemy")) continue;

            Character character = hit.GetComponent<Character>();
            if (character != null)
            {
                float dist    = Vector2.Distance(transform.position, hit.transform.position);
                float falloff = 1f - Mathf.Clamp01(dist / explosionRadius);

                // Damage giảm dần theo khoảng cách
                character.TakeDame(damage * falloff);

                // Đẩy lùi qua Character system (tránh conflict với MovePosition)
                if (knockbackForce > 0f)
                {
                    Vector2 pushDir = ((Vector2)hit.transform.position - (Vector2)transform.position);
                    if (pushDir == Vector2.zero) pushDir = Vector2.up;
                    character.ApplyKnockback(pushDir.normalized * knockbackForce * falloff);
                }

                enemiesHit++;
            }
        }
        // Phát âm thanh nổ
        float destroyDelay = 0f;
        if (explosionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(explosionSound);
            destroyDelay = explosionSound.length; // Đợi âm thanh phát xong

            // Ẩn hình ảnh và collider của lựu đạn đi
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }

        Debug.Log($"💥 BOOM! Bán kính {explosionRadius}m — {enemiesHit} kẻ địch trúng nổ!");

        // Spawn VFX nếu có
        if (explosionVFXPrefab != null)
            Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);

        // Hủy object sau khi âm thanh phát xong (nếu không có âm thanh thì delay = 0 -> hủy ngay)
        Destroy(gameObject, destroyDelay);
    }

    /// <summary>
    /// Hiển thị vùng nổ trong Scene View khi chọn object
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
