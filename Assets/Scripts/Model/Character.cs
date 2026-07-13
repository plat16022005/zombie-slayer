
using UnityEngine;

public abstract class Character: MonoBehaviour
{
    protected Rigidbody2D rb;
    protected StateCharacter state;
    public float maxHp { get; protected set; }
    public float hp { get; protected set; }
    public float attack { get; protected set; }
    public float speed { get; protected set; }
    public float defend { get; protected set; }
    protected abstract void Init();
    protected AudioSource audioSource;
    protected virtual void Awake()
    {
        Init();
        rb = GetComponent<Rigidbody2D>();
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
    protected virtual void Update()
    {
        Move();
    }
    public abstract void Attack();
    public virtual void TakeDame(float dame)
    {
        hp -= dame;
        if (hp <= 0) Die();
    }

    // ──── Knockback ────
    protected Vector2 knockbackVelocity = Vector2.zero;
    [SerializeField] private float knockbackDecay = 18f;  // Tốc độ tắt dần

    /// <summary>Áp lực đẩy lùi (gọi từ Boom hoặc nguồn ngoài)</summary>
    public virtual void ApplyKnockback(Vector2 impulse)
    {
        knockbackVelocity += impulse;
    }

    /// <summary>True khi vẫn còn đang bị đẩy lùi đáng kể</summary>
    protected bool IsKnockedBack => knockbackVelocity.magnitude > 0.1f;

    /// <summary>Gọi trong Move() để decay knockback mỗi frame</summary>
    protected void DecayKnockback()
    {
        if (!IsKnockedBack) return;
        knockbackVelocity = Vector2.MoveTowards(knockbackVelocity, Vector2.zero, knockbackDecay * Time.deltaTime);
    }

    protected abstract void Die();
    protected abstract void Move();

    /// <summary>
    /// Lật sprite trái/phải theo hướng ngang của direction.
    /// Dùng localScale để giữ đúng vị trí của child objects (súng, muzzle...).
    /// Gọi từ bất kỳ Character nào — Soldier, Enemy, ...
    /// </summary>
    public void FlipToward(Vector2 direction)
    {
        if (direction.x == 0f) return;
        Vector3 s = transform.localScale;
        s.x = direction.x < 0f ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
        transform.localScale = s;
    }
}

public enum StateCharacter
{
    Idle,
    Move,
    Attack,
    Die
}