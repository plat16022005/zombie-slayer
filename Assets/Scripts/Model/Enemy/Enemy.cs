using System.Collections;
using UnityEngine;

public abstract class Enemy : Character
{
    protected Transform player;
    protected Soldier targetSoldier;
    
    [Header("Combat Settings")]
    [Tooltip("Tầm tấn công của quái (hiển thị vòng tròn vàng trong Scene)")]
    [SerializeField] protected float attackRange = 1f;
    [Tooltip("Thời gian giữa 2 đòn đánh thường")]
    [SerializeField] protected float attackCooldown = 1f;
    [SerializeField] protected AudioClip attackSound;
    protected float attackTimer = 0f;
    [Header("Animation")]
    [SerializeField] protected EnemyAnimator enemyAnimator;

    [Header("SFX")]
    [SerializeField] protected AudioClip hurtSound;
    [SerializeField] protected AudioClip deathSound;
    [SerializeField] protected AudioClip[] moanSounds;
    [SerializeField] protected float moanIntervalMin = 4f;
    [SerializeField] protected float moanIntervalMax = 10f;
    protected float moanTimer;

    // ──── Hit Flash VFX (Shader MaterialPropertyBlock) ────
    [Header("Hit Flash VFX")]
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField, Range(0f, 1f)] private float hitFlashIntensity = 0.5f; // Mức độ chớp sáng

    // ──── Blood Splatter VFX ────
    [Header("Blood Splatter VFX")]
    [SerializeField] private ParticleSystem bloodSplatterPrefab; // Assign BloodSplatter prefab

    [Header("Die Settings")]
    [Tooltip("Khói đen bốc lên khi zombie chết.")]
    [SerializeField] protected ParticleSystem deathSmokePrefab;
    [Tooltip("Thời gian chờ trước khi bắt đầu hiệu ứng mờ dần.")]
    [SerializeField] protected float fadeWaitTime = 1f;
    [Tooltip("Thời gian chạy hiệu ứng mờ dần.")]
    [SerializeField] protected float fadeDuration = 2f;
    
    [Header("Spawn Settings")]
    [Tooltip("Thời gian đứng yên lúc mới sinh ra (dành cho animation mọc từ dưới đất)")]
    [SerializeField] protected float spawnDelay = 0f;

    [Header("Exp Drop")]
    [Tooltip("Kéo thả file Exp Database (ScriptableObject) vào đây")]
    [SerializeField] protected ExpDatabase expDatabase;
    [Tooltip("Tổng lượng kinh nghiệm rớt ra khi quái này chết")]
    [SerializeField] protected int expDropAmount = 10;

    [Header("Item Drop Options")]
    [Tooltip("Prefab của vật phẩm Hồi Máu")]
    [SerializeField] protected GameObject healItemPrefab;
    [Tooltip("Prefab của vật phẩm Nam Châm")]
    [SerializeField] protected GameObject magnetItemPrefab;
    [Tooltip("Tỷ lệ rớt Item (0.01 = 1%, 0.05 = 5%)")]
    [SerializeField] protected float itemDropChance = 0.05f;

    [Header("Movement AI Options")]
    [Tooltip("Tick vào đây để Zombie dùng thuật toán A* tìm đường (Thông minh nhưng nặng)")]
    [SerializeField] protected bool useAStar = false;
    
    [Tooltip("Tick vào đây để Zombie dùng mắt tia laser lách vật cản (Nhẹ mượt). Tắt CẢ HAI để đi thẳng cắm đầu!")]
    [SerializeField] protected bool useRaycast = true;
    
    [Header("AStar Settings")]
    protected System.Collections.Generic.List<Vector3> currentPath;
    protected int currentPathIndex;
    protected float pathUpdateTimer = 0f;
    [Tooltip("Bao lâu thì quét đường A* 1 lần")]
    [SerializeField] protected float pathUpdateInterval = 0.5f;

    [Header("Raycast Settings")]
    [Tooltip("Layer chứa vật cản để bắn Raycast")]
    [SerializeField] protected LayerMask obstacleMask;
    [Tooltip("Chiều dài tia laser dò đường")]
    [SerializeField] protected float rayDistance = 1.5f;
    [Tooltip("Độ béo (rộng) của tầm nhìn. Giúp Zombie lách mượt mà không bị cạ vai vào đá")]
    [SerializeField] protected float rayRadius = 0.5f;

    private SpriteRenderer[]    spriteRenderers;
    private MaterialPropertyBlock mpb;                        
    private static readonly int  FlashAmountID = Shader.PropertyToID("_FlashAmount"); 
    private static readonly int  FadeAmountID = Shader.PropertyToID("_FadeAmount");
    private Coroutine            hitFlashCoroutine;
    protected bool               isDead = false;
    protected bool               isSpawning = false;

    protected override void Awake()
    {
        base.Awake();
        Spawn();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();

        // Đảm bảo FlashAmount bắt đầu = 0 (bình thường)
        SetFlashAmount(0f);

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            targetSoldier = playerObject.GetComponent<Soldier>();
        }
        if (player == null)
            Debug.LogWarning("Ko tìm thấy Player");
    }
    
    protected override void Update()
    {
        if (isDead) return;
        if (isSpawning) return; // Đứng yên, không cắn, không xoay người lúc đang spawn

        // Nếu người chơi đã chết, Zombie đứng im
        if (targetSoldier != null && targetSoldier.hp <= 0)
        {
            rb.velocity = Vector2.zero;
            enemyAnimator?.UpdateState(false);
            return;
        }

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

            // Shader HitFlash
            if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = StartCoroutine(HitFlashRoutine());

            // Blood Splatter Particle
            SpawnBloodSplatter();
        }
    }

    /// <summary>Bật shader HitFlash qua MaterialPropertyBlock, rồi tắt sau hitFlashDuration.</summary>
    private IEnumerator HitFlashRoutine()
    {
        SetFlashAmount(hitFlashIntensity);
        yield return new WaitForSeconds(hitFlashDuration);
        SetFlashAmount(0f);
        hitFlashCoroutine = null;
    }

    /// <summary>Set _FlashAmount trên tất cả SpriteRenderer qua MaterialPropertyBlock (không đụng sharedMaterial).</summary>
    private void SetFlashAmount(float amount)
    {
        if (spriteRenderers == null) return;
        foreach (var sr in spriteRenderers)
        {
            if (sr == null) continue;
            sr.GetPropertyBlock(mpb);
            mpb.SetFloat(FlashAmountID, amount);
            sr.SetPropertyBlock(mpb);
        }
    }
    private void SpawnBloodSplatter()
    {
        if (bloodSplatterPrefab == null) return;

        ParticleSystem ps = Instantiate(
            bloodSplatterPrefab,
            transform.position,
            Quaternion.identity
        );
        ps.Play();
        // Tự xóa sau khi particle xong
        Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }
    
    protected override void Die()
    {
        if (hp <= 0 && !isDead)
        {
            isDead = true;

            // Hủy NGAY LẬP TỨC toàn bộ các Coroutine (đòn đánh thường, skill Húc, Nhảy, Gọi đệ...)
            // đang trong quá trình wind-up (chờ) để tránh tình trạng chết rồi vẫn gây sát thương
            StopAllCoroutines();

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

            // Bắt đầu quá trình mờ dần rồi mới hủy Object
            StartCoroutine(FadeOutRoutine());

            // Sinh ra hiệu ứng khói đen
            if (deathSmokePrefab != null)
            {
                ParticleSystem smoke = Instantiate(deathSmokePrefab, transform.position, Quaternion.Euler(-90f, 0f, 0f)); // Khói hướng lên trên (thường là Z up theo 2D)
                smoke.Play();
                Destroy(smoke.gameObject, smoke.main.duration + smoke.main.startLifetime.constantMax);
            }

            // Rớt kinh nghiệm thông minh (Tự động tính số lượng ngọc tối ưu)
            if (expDatabase != null && expDropAmount > 0)
            {
                expDatabase.SpawnExpGems(expDropAmount, transform.position);
            }

            // Tỉ lệ rớt Vật phẩm (Heal / Magnet)
            if (Random.value <= itemDropChance)
            {
                // Ngẫu nhiên 50-50 rớt Heal hoặc Magnet
                if (Random.value < 0.5f && healItemPrefab != null)
                {
                    Instantiate(healItemPrefab, transform.position, Quaternion.identity);
                }
                else if (magnetItemPrefab != null)
                {
                    Instantiate(magnetItemPrefab, transform.position, Quaternion.identity);
                }
            }
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        // 1. Chờ 1 giây trước khi mờ dần
        yield return new WaitForSeconds(fadeWaitTime);

        // 2. Chạy hiệu ứng mờ dần (Fade Out) trong fadeDuration
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float amount = Mathf.Clamp01(t / fadeDuration); 
            
            if (spriteRenderers != null && mpb != null)
            {
                foreach (var sr in spriteRenderers)
                {
                    if (sr == null) continue;
                    sr.GetPropertyBlock(mpb);
                    mpb.SetFloat(FadeAmountID, amount);
                    sr.SetPropertyBlock(mpb);
                }
            }
            yield return null;
        }

        // 3. Xoá GameObject khi hoàn tất
        Destroy(gameObject);
    }

    protected override void Move()
    {
        if (isDead) return;
        if (isSpawning) { rb.velocity = Vector2.zero; return; }

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
            if (useAStar)
            {
                MoveWithAStar();
            }
            else if (useRaycast)
            {
                MoveWithRaycast();
            }
            else
            {
                // Đi thẳng cắm đầu (không bật cả A* lẫn Raycast)
                FlipToward(player.position - transform.position);
                rb.MovePosition(Vector2.MoveTowards(rb.position, player.position, speed * Time.fixedDeltaTime));
            }
        }
    }

    private void MoveWithAStar()
    {
        // 1. Cập nhật đường đi A* mỗi khoảng thời gian
        pathUpdateTimer -= Time.deltaTime;
        if (pathUpdateTimer <= 0)
        {
            if (AStarPathfinding.Instance != null)
            {
                currentPath = AStarPathfinding.Instance.FindPath(transform.position, player.position);
                currentPathIndex = 0;
            }
            pathUpdateTimer = pathUpdateInterval;
        }

        // 2. Di chuyển theo đường đi (waypoint) A*
        if (currentPath != null && currentPathIndex < currentPath.Count)
        {
            Vector2 targetPos = currentPath[currentPathIndex];
            
            // Điểm tiếp theo quá gần -> chuyển sang điểm kế
            if (Vector2.Distance(rb.position, targetPos) < 0.2f)
            {
                currentPathIndex++;
            }
            else
            {
                FlipToward(targetPos - rb.position);
                rb.MovePosition(Vector2.MoveTowards(rb.position, targetPos, speed * Time.fixedDeltaTime));
            }
        }
        else
        {
            // Fallback: Nếu không tìm thấy đường, đi thẳng tới player như cũ
            FlipToward(player.position - transform.position);
            rb.MovePosition(Vector2.MoveTowards(rb.position, player.position, speed * Time.fixedDeltaTime));
        }
    }

    private void MoveWithRaycast()
    {
        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        Vector2 finalMoveDirection = directionToPlayer;

        // Bắn CircleCast (tia laser béo) thay vì Raycast (tia laser mỏng)
        RaycastHit2D hitFront = Physics2D.CircleCast(transform.position, rayRadius, directionToPlayer, rayDistance, obstacleMask);
        
        // Nếu phát hiện phía trước có cục đá (vật cản)
        if (hitFront.collider != null)
        {
            // Thử nhìn sang Trái (Bẻ góc 60 độ)
            Vector2 leftDir = Quaternion.Euler(0, 0, 45) * directionToPlayer;
            RaycastHit2D hitLeft = Physics2D.CircleCast(transform.position, rayRadius, leftDir, rayDistance, obstacleMask);

            // Thử nhìn sang Phải (Bẻ góc -60 độ)
            Vector2 rightDir = Quaternion.Euler(0, 0, -45) * directionToPlayer;
            RaycastHit2D hitRight = Physics2D.CircleCast(transform.position, rayRadius, rightDir, rayDistance, obstacleMask);

            // Quyết định hướng rẽ
            if (hitLeft.collider == null) 
            {
                finalMoveDirection = leftDir; // Bên trái trống -> Lách sang trái
            }
            else if (hitRight.collider == null)
            {
                finalMoveDirection = rightDir; // Bên phải trống -> Lách sang phải
            }
            else 
            {
                // Cả hai bên đều kẹt -> Bẻ cua gắt 90 độ luôn để trượt dọc bờ tường
                finalMoveDirection = Quaternion.Euler(0, 0, 90) * directionToPlayer;
            }
        }

        FlipToward(finalMoveDirection);
        // Thay vì dùng MoveTowards, ở đây dùng rb.position + hướng di chuyển để nó dạt sang 2 bên
        rb.MovePosition(rb.position + finalMoveDirection * speed * Time.fixedDeltaTime);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // Vẽ vòng tròn màu vàng thể hiện tầm đánh (attackRange) trong cửa sổ Scene
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    // ──── Spawn System ────
    public virtual void Spawn()
    {
        StartCoroutine(SpawnRoutine());
    }

    protected virtual IEnumerator SpawnRoutine()
    {
        isSpawning = true;
        
        // Khóa va chạm tạm thời để không bị ăn đạn lúc vừa ngoi lên
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Bật animation Spawn (nếu có)
        // enemyAnimator?.PlaySpawn();

        yield return new WaitForSeconds(spawnDelay);

        // Bật lại
        if (col != null) col.enabled = true;
        isSpawning = false;
    }
}
