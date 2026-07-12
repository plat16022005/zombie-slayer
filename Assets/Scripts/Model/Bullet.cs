using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed    = 15f;
    [SerializeField] private float damage   = 10f;
    [SerializeField] private float lifeTime = 3f;

    [Header("VFX")]
    [SerializeField] private GameObject hitEffectPrefab;   // Particle nổ khi trúng
    [SerializeField] private AudioClip  bulletSound;     // Âm thanh đạn va chạm (nếu có)
    private AudioSource audioSource;
    private Vector2 moveDirection;

    private void Awake()
    {
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
    public void Init(Vector2 direction)
    {
        moveDirection = direction.normalized;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // KHI VỪA SPAWN: Quét ngay tại chỗ xem có đang nằm đè lên quái vật nào không
        // Giúp chống lỗi đạn spawn thẳng vào bên trong Collider quái vật và bay xuyên qua luôn
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, 0.2f);
        foreach (var col in hitColliders)
        {
            if (col.GetComponent<Enemy>() != null)
            {
                OnTriggerEnter2D(col); // Xử lý trúng đạn ngay lập tức
                break;
            }
        }

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<Enemy>(out Enemy enemy))
        {
            enemy.TakeDame(damage);
            SpawnHitEffect();

            // Kích hoạt Hit Stop (dừng hình chớp nhoáng)
            if (GameFeelManager.Instance != null)
                GameFeelManager.Instance.HitStop(0.03f);

            float destroyDelay = 0f;
            if (bulletSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(bulletSound);
                destroyDelay = bulletSound.length; // Chờ âm thanh phát xong

                // Ẩn hình ảnh và tắt va chạm của đạn
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;
                
                Collider2D col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;

                // Dừng viên đạn lại tại chỗ
                speed = 0f;
            }

            Destroy(gameObject, destroyDelay);
        }
    }

    /// <summary>Spawn particle VFX tại điểm trúng, xoay ngược chiều đạn bay</summary>
    private void SpawnHitEffect()
    {
        if (hitEffectPrefab == null) return;

        // Xoay effect ngược chiều đạn (như bắn tóe ra)
        Quaternion hitRot = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(-moveDirection.y, -moveDirection.x) * Mathf.Rad2Deg);

        Instantiate(hitEffectPrefab, transform.position, hitRot);
    }
}
