using UnityEngine;

public abstract class Enemy : Character
{
    protected Transform player;
    [SerializeField] protected float attackCooldown = 1f;
    protected float attackTimer = 0f;
    [SerializeField] protected float attackRange = 1f;
    [SerializeField] protected AudioClip attackSound;

    [Header("SFX")]
    [SerializeField] protected AudioClip hurtSound;
    [SerializeField] protected AudioClip deathSound;
    [SerializeField] protected AudioClip[] moanSounds; // Rên rỉ ngẫu nhiên
    [SerializeField] protected float moanIntervalMin = 4f;
    [SerializeField] protected float moanIntervalMax = 10f;
    protected float moanTimer;

    protected override void Awake()
    {
        base.Awake();
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        if (player == null)
            Debug.LogWarning("Ko tìm thấy Player");        
    }
    
    protected override void Update()
    {
        attackTimer -= Time.deltaTime;
        base.Update();
        Attack();

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
        if (player == null || attackTimer > 0)
            return;

        float distance = Vector2.Distance(rb.position, player.position);

        if (distance < attackRange)
        {
            state = StateCharacter.Attack;
            // Tấn công Player
            Soldier soldier = player.GetComponent<Soldier>();
            if (soldier != null)
            {
                soldier.TakeDame(attack);
                Debug.Log($"Đang đánh Soldier: {gameObject.name}");
                if (attackSound != null && audioSource != null)
                    audioSource.PlayOneShot(attackSound);
            }
            attackTimer = attackCooldown;
        }
    }

    public override void TakeDame(float dame)
    {
        base.TakeDame(dame);
        Debug.Log($" {gameObject.name} nhận {dame} dame, còn {hp} hp");

        if (hp > 0 && hurtSound != null && audioSource != null)
            audioSource.PlayOneShot(hurtSound);
    }
    
    protected override void Die()
    {
        if (hp <= 0) 
        {
            float destroyDelay = 0f;
            if (deathSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(deathSound);
                destroyDelay = deathSound.length; // Chờ âm thanh phát xong

                // Ẩn enemy và tắt va chạm
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;
                
                Collider2D col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
                
                // Tắt script này
                this.enabled = false; 
            }
            
            Destroy(this.gameObject, destroyDelay);
        }
    }

    protected override void Move()
    {
        DecayKnockback();

        // Đang bị đẩy lùi → để physics tự xử lý, không snap về phía player
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
