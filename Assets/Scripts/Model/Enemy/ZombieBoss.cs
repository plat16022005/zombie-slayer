using System.Collections;
using UnityEngine;

public class ZombieBoss : Zombie
{
    [Header("Boss Skills: General")]
    [Tooltip("Thời gian chờ tối thiểu giữa 2 lần tung chiêu")]
    [SerializeField] private float minSkillCooldown = 5f;
    [Tooltip("Thời gian chờ tối đa giữa 2 lần tung chiêu")]
    [SerializeField] private float maxSkillCooldown = 10f;
    [Tooltip("Nhạc nền riêng của Boss này")]
    [SerializeField] private AudioClip bossBGM;

    [Header("Boss Skills: Charge (Húc)")]
    [SerializeField] private AudioClip chargeSound;
    [SerializeField] private float chargeSpeedMultiplier = 3.5f;
    [SerializeField] private float chargeDuration = 1f;
    [SerializeField] private float chargeDamageMultiplier = 2f;
    [Tooltip("Hiệu ứng bụi khói kéo dài sau lưng khi lướt (Gắn trực tiếp vào Boss)")]
    [SerializeField] private ParticleSystem chargeTrailVFX;

    [Header("Boss Skills: Summon (Gọi Đệ)")]
    [SerializeField] private AudioClip summonSound;
    [Tooltip("Prefab của quái nhỏ (Zombie thường) để boss gọi ra")]
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private int minionCount = 3;
    [Tooltip("VFX xuất hiện dưới chân quái nhỏ lúc được triệu hồi")]
    [SerializeField] private GameObject summonVFXPrefab;

    [Header("Boss Skills: Jump (Nhảy đập đất)")]
    [SerializeField] private AudioClip jumpSlamSound;
    [Tooltip("Thời gian boss lơ lửng trên không")]
    [SerializeField] private float jumpDuration = 1f;
    [Tooltip("Bán kính sát thương khi chạm đất")]
    [SerializeField] private float jumpAoERadius = 3f;
    [Tooltip("Hệ số sát thương (Attack x Multiplier)")]
    [SerializeField] private float jumpDamageMultiplier = 3f;
    [Tooltip("Độ cao (tỷ lệ phình to sprite) khi nhảy")]
    [SerializeField] private float jumpHeightScale = 0.5f; 
    [Tooltip("Hiệu ứng bụi/khói bùng nổ khi đáp đất (có thể chứa nhiều lớp Particle)")]
    [SerializeField] private GameObject jumpSlamVFXPrefab;

    private float skillTimer;
    private bool isCharging = false;
    private bool isDashing = false;
    private bool isSummoning = false;
    private bool isJumping = false;
    
    [Header("Boss Arena")]
    [Tooltip("Prefab của lồng giam nhốt Player và Boss")]
    [SerializeField] private GameObject arenaPrefab;
    private GameObject activeArena;

    private ZombieBossAnimator bossAnimator;
    

    protected override void Init()
    {
        spawnDelay = 3f;
        // Khởi tạo chỉ số cơ bản của Boss
        state = StateCharacter.Idle;
        maxHp = 20000;
        hp = maxHp;
        attack = 25;
        speed = 8;
        defend = 0;
        moanTimer = Random.Range(moanIntervalMin, moanIntervalMax);
        
        bossAnimator = enemyAnimator as ZombieBossAnimator;
        
        // Random khoảng thời gian tung chiêu đầu tiên
        skillTimer = Random.Range(minSkillCooldown, maxSkillCooldown);
    }

    protected override void Awake()
    {
        base.Awake(); // Gọi Awake của Enemy để nạp chỉ số và lấy tọa độ Player
        if (player != null)
        {
            Vector2 randomOffset = Random.insideUnitCircle.normalized * 10f;
            transform.position = (Vector2)player.position + randomOffset;
        }
    }

    public override void Spawn()
    {
        // Quét tất cả Enemy đang có trên bản đồ và tiêu diệt ngay lập tức
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in allEnemies)
        {
            if (enemyObj == this.gameObject) continue; // Bỏ qua chính bản thân Boss
            
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy != null && enemy.hp > 0)
            {
                enemy.TakeDame(99999f); // Sát thương khủng để chết ngay
            }
        }

        base.Spawn();
        // Gọi UI nháy đỏ màn hình và truyền bài nhạc của Boss vào để UI tự phát
        if (PlayerUIManager.Instance != null)
        {
            PlayerUIManager.Instance.ShowBossWarning(bossBGM);
        }
    }

    protected virtual void Start()
    {
        // Tắt các Spawner đẻ vật thể/ngọc rác trên bản đồ để tránh vướng víu đánh Boss
        InfiniteObstacleSpawner[] obsSpawners = FindObjectsOfType<InfiniteObstacleSpawner>();
        foreach (var obs in obsSpawners) obs.enabled = false;

        ExpSpawner[] expSpawners = FindObjectsOfType<ExpSpawner>();
        foreach (var exp in expSpawners) exp.enabled = false;

        if (arenaPrefab != null)
        {
            activeArena = Instantiate(arenaPrefab, transform.position, Quaternion.identity);
        }
    }

    protected override void Die()
    {
        if (hp <= 0 && !isDead)
        {
            // Xóa lồng khi boss chết
            if (activeArena != null)
            {
                Destroy(activeArena);
            }
            
            // Gọi hàm Die gốc của Enemy
            base.Die();
        }
    }

    protected override void Update()
    {
        if (hp <= 0) return;

        // Dừng boss hoàn toàn nếu người chơi đã chết
        if (targetSoldier != null && targetSoldier.hp <= 0)
        {
            rb.velocity = Vector2.zero;
            enemyAnimator?.UpdateState(false);
            StopAllCoroutines(); // Hủy các chiêu đang vận
            isCharging = isSummoning = isJumping = isDashing = false;
            return;
        }

        // Nếu đang trong trạng thái tung chiêu, bỏ qua Update bình thường
        if (isCharging || isSummoning || isJumping)
        {
            attackTimer -= Time.deltaTime;
            
            if (isCharging && isDashing)
            {
                ChargeMove();
                ChargeAttack();
            }
            return;
        }

        // Không trừ timer hồi chiêu khi bị Knockback
        if (!IsKnockedBack)
        {
            skillTimer -= Time.deltaTime;

            // Đã tới lúc tung chiêu
            if (skillTimer <= 0)
            {
                // Bốc thăm chiêu ngẫu nhiên (0: Húc, 1: Gọi đệ, 2: Nhảy)
                int randSkill = Random.Range(0, 3);
                
                if (randSkill == 1 && minionPrefab != null)
                {
                    StartCoroutine(SummonRoutine());
                }
                else if (randSkill == 2)
                {
                    StartCoroutine(JumpRoutine());
                }
                else
                {
                    StartCoroutine(ChargeRoutine());
                }
                return;
            }
        }

        // Nếu không xài chiêu, hoạt động như quái thường
        base.Update();
    }

    private Vector2 chargeDirection;

    private IEnumerator ChargeRoutine()
    {
        isCharging = true;
        isDashing = false;

        rb.velocity = Vector2.zero;

        // 1. Chốt hướng lao NGAY TỪ ĐẦU (để hiện cảnh báo)
        if (player != null)
        {
            chargeDirection = (player.position - transform.position).normalized;
            FlipToward(chargeDirection);
        }
        else
        {
            chargeDirection = Vector2.left; // Mặc định nếu mất dấu
        }

        // Tính toán quãng đường Boss sẽ bay tới
        float dashDistance = speed * chargeSpeedMultiplier * chargeDuration;
        
        // Chiều rộng đường bay = Đường kính vùng sát thương (bán kính lúc húc là attackRange * 1.5f)
        float warningWidth = attackRange * 1.5f * 2f;

        // HIỂN THỊ ĐƯỜNG BAY CẢNH BÁO
        ShowChargeWarning(rb.position, chargeDirection, dashDistance, warningWidth, 1f + chargeDuration);

        // 2. Wind-up (Khởi động chiêu) - Khựng lại 1s để cảnh báo
        enemyAnimator?.UpdateState(false);
        if (bossAnimator != null) bossAnimator.PlayCharge();
        else enemyAnimator?.PlayAttack();
        yield return new WaitForSeconds(1f); 

        // 3. Dash (Lao đi)
        isDashing = true;
        if (chargeTrailVFX != null) chargeTrailVFX.Play();
        float originalSpeed = speed;
        speed *= chargeSpeedMultiplier;
        
        // Phát tiếng gầm
        if (chargeSound != null && audioSource != null)
            audioSource.PlayOneShot(chargeSound);

        yield return new WaitForSeconds(chargeDuration);

        // 4. Kết thúc Dash
        if (chargeTrailVFX != null) chargeTrailVFX.Stop();
        speed = originalSpeed;
        isCharging = false;
        isDashing = false;
        rb.velocity = Vector2.zero; // Dừng hẳn ngay lập tức để không bị trôi
        
        // Đặt lại thời gian ngẫu nhiên cho lần tung chiêu kế tiếp
        skillTimer = Random.Range(minSkillCooldown, maxSkillCooldown);
    }

    private void ChargeMove()
    {
        // Ép animation chạy
        enemyAnimator?.UpdateState(true);

        // Lao thẳng theo hướng ĐÃ CHỐT cố định (dùng velocity thay vì MovePosition để tính chính xác khoảng cách)
        rb.velocity = chargeDirection * speed;
    }

    private void ChargeAttack()
    {
        if (player == null || attackTimer > 0) return;
        
        float distance = Vector2.Distance(rb.position, player.position);
        // Tầm đánh rộng hơn khi húc
        if (distance < attackRange * 1.5f)
        {
            Soldier soldier = player.GetComponent<Soldier>();
            if (soldier != null)
            {
                soldier.TakeDame(attack * chargeDamageMultiplier);
                
                // Đẩy lùi mạnh (Knockback)
                Vector2 knockbackDir = (player.position - transform.position).normalized;
                soldier.ApplyKnockback(knockbackDir * 20f); // Đẩy 15 units
                
                enemyAnimator?.PlayAttack();
                if (attackSound != null && audioSource != null)
                    audioSource.PlayOneShot(attackSound);
            }
            // Húc trúng 1 lần, chờ nửa thời gian cooldown để có thể húc tiếp nếu player áp sát
            attackTimer = attackCooldown * 0.5f; 
        }
    }

    private IEnumerator SummonRoutine()
    {
        isSummoning = true;

        // Dừng di chuyển
        rb.velocity = Vector2.zero;
        enemyAnimator?.UpdateState(false);
        
        // Animation
        if (bossAnimator != null) bossAnimator.PlaySummon();
        else enemyAnimator?.PlayAttack();
        
        // Tiếng gọi đệ
        if (summonSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(summonSound);
        }

        yield return new WaitForSeconds(1f);

        // Sinh quái
        for (int i = 0; i < minionCount; i++)
        {
            Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * 3f;
            GameObject minion = Instantiate(minionPrefab, spawnPos, Quaternion.identity);
            
            if (minion.tag != "Enemy") minion.tag = "Enemy";

            // Sinh hiệu ứng triệu hồi tại đúng chỗ quái con xuất hiện
            if (summonVFXPrefab != null)
            {
                GameObject vfx = Instantiate(summonVFXPrefab, spawnPos, Quaternion.identity);
                ParticleSystem[] particles = vfx.GetComponentsInChildren<ParticleSystem>();
                foreach(var p in particles) p.Play();
                Destroy(vfx, 2f); // Tự hủy sau 2 giây
            }
        }

        yield return new WaitForSeconds(1f); // Delay nhỏ trước khi trở lại bình thường
        isSummoning = false;
        
        // Đặt lại thời gian ngẫu nhiên cho lần tung chiêu kế tiếp
        skillTimer = Random.Range(minSkillCooldown, maxSkillCooldown);
    }

    private IEnumerator JumpRoutine()
    {
        isJumping = true;

        if (player == null) 
        {
            isJumping = false;
            skillTimer = Random.Range(minSkillCooldown, maxSkillCooldown);
            yield break;
        }

        // Lấy vị trí bắt đầu và đích đến NGAY LÚC CHUẨN BỊ
        Vector2 startPos = rb.position;
        Vector2 targetPos = player.position;
        
        // Quay mặt về hướng nhảy
        FlipToward(targetPos - startPos);

        // HIỂN THỊ VÒNG TRÒN CẢNH BÁO
        ShowJumpWarning(targetPos, jumpAoERadius, 0.5f + jumpDuration);

        // 1. Dừng lại, nhún người chuẩn bị nhảy
        rb.velocity = Vector2.zero;
        enemyAnimator?.UpdateState(false);
        if (bossAnimator != null) bossAnimator.PlayJump();
        else enemyAnimator?.PlayAttack(); // Tạm dùng PlayAttack làm animation khởi động
        yield return new WaitForSeconds(1f);

        // Tắt collider để bay qua mọi chướng ngại vật
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        float t = 0f;
        Vector3 baseScale = transform.localScale;
        float signX = Mathf.Sign(baseScale.x);
        float absX = Mathf.Abs(baseScale.x);
        float baseY = baseScale.y;

        // 2. Quá trình lơ lửng trên không
        while (t < jumpDuration)
        {
            t += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(t / jumpDuration);

            // Nội suy vị trí từ Start đến Target
            Vector2 currentPos = Vector2.Lerp(startPos, targetPos, normalizedTime);
            rb.MovePosition(currentPos);

            // Giả lập nhảy trên mặt phẳng 2D bằng cách phóng to sprite ở giữa chu kỳ (Parabol)
            float heightCurve = 4f * normalizedTime * (1f - normalizedTime); // max = 1 tại t = 0.5
            float scaleModifier = 1f + (heightCurve * jumpHeightScale);
            
            transform.localScale = new Vector3(
                signX * absX * scaleModifier,
                baseY * scaleModifier,
                baseScale.z
            );

            yield return null;
        }

        // 3. Đập đất
        rb.MovePosition(targetPos);
        transform.localScale = new Vector3(signX * absX, baseY, baseScale.z); // Trả lại Scale gốc
        if (col != null) col.enabled = true; // Bật lại collider

        // Phát hoạt ảnh và âm thanh đập đất
        // enemyAnimator?.PlayAttack();
        if (jumpSlamSound != null && audioSource != null)
            audioSource.PlayOneShot(jumpSlamSound);

        // Sinh ra hiệu ứng khói bụi bùng nổ
        if (jumpSlamVFXPrefab != null)
        {
            GameObject slamVFX = Instantiate(jumpSlamVFXPrefab, targetPos, Quaternion.identity);
            
            // Tìm và play tất cả các lớp Particle nằm trong khối VFX này
            ParticleSystem[] allParticles = slamVFX.GetComponentsInChildren<ParticleSystem>();
            foreach(var ps in allParticles)
            {
                ps.Play();
            }

            // Hủy sau 3 giây (đủ thời gian cho các loại khói tan hết)
            Destroy(slamVFX, 3f);
        }

        // Gây sát thương AoE (Hình tròn xung quanh vị trí đáp xuống)
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, jumpAoERadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Soldier soldier = hit.GetComponent<Soldier>();
                if (soldier != null)
                {
                    // Trừ máu theo hệ số
                    soldier.TakeDame(attack * jumpDamageMultiplier);
                    
                    // Hất văng người chơi
                    Vector2 knockbackDir = ((Vector2)hit.transform.position - targetPos).normalized;
                    if (knockbackDir == Vector2.zero) knockbackDir = Random.insideUnitCircle.normalized; // Tránh trường hợp đứng trùng vị trí tuyệt đối
                    soldier.ApplyKnockback(knockbackDir * 25f); // Hất rất mạnh
                }
            }
        }

        // Tạm nghỉ 0.5s sau khi đập đất để player khỏi bị bồi thêm ngay lập tức
        yield return new WaitForSeconds(0.5f);

        isJumping = false;
        attackTimer = attackCooldown; // Reset đòn đánh thường
        
        // Random thời gian cho chiêu tiếp theo
        skillTimer = Random.Range(minSkillCooldown, maxSkillCooldown);
    }
    
    // Hàm vẽ vùng sát thương đập đất trong cửa sổ Scene (để dễ thiết kế)
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected(); // Vẽ vòng màu vàng (tầm đánh thường) từ class Enemy
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, jumpAoERadius); // Vòng màu đỏ (tầm đập đất)
    }

    // Hàm tạo vòng tròn cảnh báo bằng LineRenderer hiển thị khi chơi thật
    private void ShowJumpWarning(Vector2 position, float radius, float duration)
    {
        GameObject warningObj = new GameObject("JumpWarning");
        warningObj.transform.position = position;

        LineRenderer lr = warningObj.AddComponent<LineRenderer>();
        // Sử dụng thủ thuật LineRenderer siêu ngắn + numCapVertices để vẽ hình tròn ĐẶC (Tô kín)
        lr.startWidth = radius * 2f;
        lr.endWidth = radius * 2f;
        lr.numCapVertices = 50; // Bo tròn hoàn hảo 2 đầu
        lr.positionCount = 2;
        
        // Hai điểm cực gần nhau sẽ tạo thành một hình tròn hoàn chỉnh
        lr.SetPosition(0, new Vector3(position.x, position.y, 0f));
        lr.SetPosition(1, new Vector3(position.x + 0.001f, position.y, 0f));

        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0f, 0f, 0.4f); // Đỏ tô kín (bán trong suốt)
        lr.endColor = new Color(1f, 0f, 0f, 0.4f);
        lr.useWorldSpace = true;
        lr.sortingOrder = -10; // Vẽ chìm dưới đất

        // Tự động xóa vòng tròn khi Boss đáp đất
        Destroy(warningObj, duration);
    }

    // Hàm tạo đường thẳng cảnh báo bằng LineRenderer cho chiêu Charge
    private void ShowChargeWarning(Vector2 startPos, Vector2 direction, float distance, float width, float duration)
    {
        GameObject warningObj = new GameObject("ChargeWarning");
        warningObj.transform.position = startPos;

        LineRenderer lr = warningObj.AddComponent<LineRenderer>();
        lr.startWidth = width;
        lr.endWidth = width;
        // Dùng shader mặc định để vẽ màu cơ bản
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0.5f, 0f, 0.3f); // Màu cam bán trong suốt
        lr.endColor = new Color(1f, 0.5f, 0f, 0.3f);
        lr.positionCount = 2; // Đường thẳng 2 điểm
        lr.useWorldSpace = true;
        lr.sortingOrder = -10; // Vẽ chìm dưới đất

        Vector2 endPos = startPos + direction * distance;
        lr.SetPosition(0, new Vector3(startPos.x, startPos.y, 0f));
        lr.SetPosition(1, new Vector3(endPos.x, endPos.y, 0f));

        // Tự động xóa cảnh báo khi hết duration
        Destroy(warningObj, duration);
    }
}
