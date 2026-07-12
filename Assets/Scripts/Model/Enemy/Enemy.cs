using System.Collections;
using UnityEngine;

public abstract class Enemy : Character
{
    protected Transform player;
    [SerializeField] protected float attackCooldown = 1f;
    protected float attackTimer = 0f;
    [SerializeField] protected float attackRange = 1f;
    [SerializeField] protected AudioClip attackSound;

    [Header("Animation")]
    [SerializeField] protected EnemyAnimator enemyAnimator;

    [Header("SFX")]
    [SerializeField] protected AudioClip hurtSound;
    [SerializeField] protected AudioClip deathSound;
    [SerializeField] protected AudioClip[] moanSounds;
    [SerializeField] protected float moanIntervalMin = 4f;
    [SerializeField] protected float moanIntervalMax = 10f;
    protected float moanTimer;

    // ──── Hit Flash VFX ────
    [Header("Hit Flash VFX")]
    [SerializeField] private Color  hitFlashColor    = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private float  hitFlashDuration = 0.15f;

    [Header("Die Settings")]
    [Tooltip("Thời gian chờ (giây) để animation chết chạy xong trước khi Destroy.\nĐặt = độ dài clip Die.")]
    [SerializeField] protected float dieDestroyDelay = 2f;

    private SpriteRenderer[] spriteRenderers;
    private Coroutine        hitFlashCoroutine;
    private bool             isDead = false;

    protected override void Awake()
    {
        base.Awake();

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
        if (player == null)
            Debug.LogWarning("Ko tìm thấy Player");
    }
    
    protected override void Update()
    {
        if (isDead) return;

        attackTimer -= Time.deltaTime;
        base.Update();
        Attack();

        // Cập nhật animation di chuyển mỗi frame
        bool isMoving = player != null
                     && !IsKnockedBack
                     && Vector2.Distance(rb.position, player.position) >= attackRange;
        enemyAnimator?.UpdateState(isMoving);

        // Tiếng rên rỉ ngẫu nhiên
        if (hp > 0)
        {
            moanTimer -= Time.deltaTime;
            if (moanTimer <= 0)
            {
                if (moanSounds != null && moanSounds.Length > 0 && audioSource != null)
                {
                    AudioClip moan = moanSounds[Random.Range(0, moanSounds.Length)];
                    audioSource.PlayOneShot(moan);
                }
                moanTimer = Random.Range(moanIntervalMin, moanIntervalMax);
            }
        }
    }

    public override void Attack()
    {
        if (isDead) return;
        if (player == null || attackTimer > 0) return;

        float distance = Vector2.Distance(rb.position, player.position);

        if (distance < attackRange)
        {
            state = StateCharacter.Attack;
            Soldier soldier = player.GetComponent<Soldier>();
            if (soldier != null)
            {
                soldier.TakeDame(attack);
                enemyAnimator?.PlayAttack();
                Debug.Log($"Đang đánh Soldier: {gameObject.name}");
                if (attackSound != null && audioSource != null)
                    audioSource.PlayOneShot(attackSound);
            }
            attackTimer = attackCooldown;
        }
    }

    public override void TakeDame(float dame)
    {
        if (isDead) return;

        base.TakeDame(dame);
        Debug.Log($" {gameObject.name} nhận {dame} dame, còn {hp} hp");

        if (hp > 0)
        {
            enemyAnimator?.PlayHit();

            if (hurtSound != null && audioSource != null)
                audioSource.PlayOneShot(hurtSound);

            // Hiệu ứng đỏ khi bị đánh
            if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
        }
    }

    /// <summary>Lerp SpriteRenderer sang đỏ rồi về trắng.</summary>
    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) yield break;

        float half = hitFlashDuration * 0.5f;
        float t = 0f;

        // Fade vào đỏ
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            Color c = Color.Lerp(Color.white, hitFlashColor, t);
            foreach (var sr in spriteRenderers) if (sr != null) sr.color = c;
            yield return null;
        }

        // Fade trở lại trắng
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            Color c = Color.Lerp(hitFlashColor, Color.white, t);
            foreach (var sr in spriteRenderers) if (sr != null) sr.color = c;
            yield return null;
        }

        foreach (var sr in spriteRenderers) if (sr != null) sr.color = Color.white;
        hitFlashCoroutine = null;
    }
    
    protected override void Die()
    {
        if (hp <= 0 && !isDead)
        {
            isDead = true;

            // Dừng vật lý ngay
            rb.velocity        = Vector2.zero;
            rb.isKinematic     = true;

            // Tắt collider để đạn không trúng thêm
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            // Play animation & sound
            enemyAnimator?.PlayDie();
            if (deathSound != null && audioSource != null)
                audioSource.PlayOneShot(deathSound);

            // Destroy sau khi animation chạy xong
            Destroy(gameObject, dieDestroyDelay);
        }
    }

    protected override void Move()
    {
        if (isDead) return;

        DecayKnockback();

        if (IsKnockedBack)
        {
            rb.velocity = knockbackVelocity;
            return;
        }

        rb.velocity = Vector2.zero;

        if (player == null) return;

        float distance = Vector2.Distance(rb.position, player.position);
        if (distance >= attackRange)
        {
            FlipToward(player.position - transform.position);

            rb.MovePosition(
                Vector2.MoveTowards(
                    rb.position,
                    player.position,
                    speed * Time.fixedDeltaTime
                )
            );
        }
    }
}
