
using System.Collections;
using UnityEngine;

public class Soldier : Character
{
    private float horizontal;
    private float vertical;
    private Vector2 lastDirection = Vector2.right;
    // ──── Recoil ────
    private Vector2 recoilVelocity = Vector2.zero;
    [SerializeField] private float recoilDecay = 20f;  // Tốc độ tắt dần giật lùi


    // ──── Animation ────
    [Header("Animation")]
    [SerializeField] private SoldierAnimator soldierAnimator;
    [SerializeField] private int idleSkinIndex = 0;

    // ──── SFX ────
    [Header("SFX")]
    [SerializeField] private AudioClip[] hitSounds;  // Chọn ngẫu nhiên khi bị đánh
    [SerializeField] private AudioClip   dieSound;   // Tiếng chết
    [Tooltip("Độ trễ (giây) trước khi Destroy — đặt bằng độ dài clip animation chết")]
    [SerializeField] private float       dieDestroyDelay = 1.5f;

    // ──── Hit Flash VFX ────
    [Header("Hit Flash VFX")]
    [SerializeField] private Color  hitFlashColor    = new Color(1f, 0.15f, 0.15f, 1f); // Màu đỏ khi bị đánh
    [SerializeField] private float  hitFlashDuration = 0.15f;  // Tổng thời gian hiệu ứng (giây)

    private SpriteRenderer[] spriteRenderers;  // Tất cả renderer trên người & vũ khí
    private Coroutine        hitFlashCoroutine; // Theo dõi để hủy khi bị đánh liên tiếp

    private bool isDead = false;  // Khóa mọi hành động sau khi chết

    // ──── Weapon System ────
    [Header("Weapon System")]
    [SerializeField] private GameObject[] weaponPrefabs;   // Danh sách prefab súng
    [SerializeField] private Transform weaponHolder;        // Điểm gắn súng lên người
    [SerializeField] private AudioClip switchGunSound;
    [Tooltip("Độ trễ (giây) từ lúc trigger Attack đến khi đạn spawn.\nĐặt = 0 nếu bạn dùng Animation Event thay thế.")]
    [SerializeField] private float fireDelay = 0.1f;        // Chờ animation đưa súng lên đúng vị trí

    private Gun currentGun;         // Instance súng đang dùng
    private int currentWeaponIndex = 0;
    private Vector2 pendingFireDirection;                   // Hướng bắn đã tính, dùng cho Animation Event

    // ──── Boom System ────
    [Header("Boom System")]
    [SerializeField] private GameObject boomPrefab;         // Prefab lựu đạn
    [SerializeField] private Transform  throwPoint;         // Điểm xuất phát khi ném
    [SerializeField] private float      boomCooldown = 10f; // Thời gian hồi chiêu của Boom
    [SerializeField] private AudioClip  boomSound;          // Tiếng ném boom

    private float boomCooldownTimer = 0f;

    // ──── Dash System ────
    [Header("Dash System")]
    [SerializeField] private float     dashSpeed    = 25f;  // Tốc độ lướt
    [SerializeField] private float     dashDuration = 0.2f; // Lướt trong bao lâu
    [SerializeField] private float     dashCooldown = 5f;   // Thời gian hồi chiêu
    [SerializeField] private AudioClip dashSound;           // Âm thanh lướt
    [SerializeField] private TrailRenderer dashTrail;       // Vệt lướt (Trail Renderer)

    private float dashCooldownTimer = 0f;
    private float dashTimer = 0f;
    private bool  isDashing = false;
    private Vector2 dashDirection;

    protected override void Init()
    {
        if (DataGame.Instance != null && DataGame.Instance.HasProfile)
        {
            maxHp = DataGame.Instance.MaxHp;
            attack = DataGame.Instance.Attack;
            speed = DataGame.Instance.Speed;
            defend = DataGame.Instance.Defend;
            Debug.Log($"Đã tải chỉ số cho Soldier từ DataGame! (MaxHp: {maxHp}, Attack: {attack})");
        }
        else
        {
            Debug.LogWarning("Chưa load DataGame! Sử dụng chỉ số mặc định để test.");
            maxHp = 100;
            attack = 20;
            speed = 10;
            defend = 5;
        }
        
        hp = maxHp; // Khởi tạo máu hiện tại bằng máu tối đa
        fireSpeed = 1f; // Tốc độ bắn cơ bản ban đầu là 1x (có thể thay đổi trong game)
    }

    protected override void Awake()
    {
        base.Awake();
        EquipWeapon(currentWeaponIndex);
        boomCooldownTimer = 0f; // Sẵn sàng ném ngay từ đầu
        dashCooldownTimer = 0f; // Sẵn sàng lướt
        if (dashTrail != null) dashTrail.emitting = false;

        // Khởi tạo animation idle đúng theo skin
        soldierAnimator?.Init(idleSkinIndex);

        // Thu thập tất cả SpriteRenderer trên người + vũ khí (dùng cho hit flash)
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    protected override void Update()
    {
        base.Update();
        
        // Cập nhật đếm ngược thời gian hồi Boom
        if (boomCooldownTimer > 0)
        {
            boomCooldownTimer -= Time.deltaTime;
        }

        // Cập nhật đếm ngược thời gian hồi Dash
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        // Xử lý trạng thái đang lướt
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
                if (dashTrail != null) dashTrail.emitting = false; // Tắt vệt lướt
            }
        }

        // Cập nhật animation mỗi frame
        soldierAnimator?.UpdateState(rb.velocity, isDashing, idleSkinIndex);
    }
    public override void TakeDame(float dame)
    {
        base.TakeDame(dame);

        // Chỉ play hit khi vẫn còn sống (tránh đè lên animation Die)
        if (hp > 0)
        {
            soldierAnimator?.PlayHit();

            // Phát tiếng bị đánh (random trong mảng nếu có nhiều clip)
            if (hitSounds != null && hitSounds.Length > 0 && audioSource != null)
            {
                AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
                if (clip != null) audioSource.PlayOneShot(clip);
            }

            // Hiệu ứng đỏ khi bị đánh
            if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
        }
    }

    /// <summary>Lerp tất cả SpriteRenderer sang đỏ rồi trả về trắng.</summary>
    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) yield break;

        float half = hitFlashDuration * 0.5f;
        float t    = 0f;

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

        // Đảm bảo reset chính xác về trắng
        foreach (var sr in spriteRenderers) if (sr != null) sr.color = Color.white;
        hitFlashCoroutine = null;
    }
    /// <summary>
    /// Trang bị súng theo index — Destroy cũ, Instantiate mới vào weaponHolder
    /// </summary>
    private void EquipWeapon(int index)
    {
        if (weaponPrefabs == null || weaponPrefabs.Length == 0)
        {
            Debug.LogWarning("Chưa gán weaponPrefabs! Kéo prefab súng vào Inspector.");
            return;
        }

        // Destroy súng đang đeo (nếu có)
        if (currentGun != null)
        {
            Destroy(currentGun.gameObject);
            currentGun = null;
        }

        // Instantiate prefab mới vào weaponHolder
        Transform parent = weaponHolder != null ? weaponHolder : transform;
        GameObject gunObj = Instantiate(weaponPrefabs[index], parent);
        gunObj.transform.localPosition = Vector3.zero;
        gunObj.transform.localRotation = Quaternion.identity;

        currentGun = gunObj.GetComponent<Gun>();
        if (currentGun == null)
            Debug.LogError($"Prefab '{weaponPrefabs[index].name}' không có component Gun!");
        else
            Debug.Log($"🔫 Trang bị: {weaponPrefabs[index].name}");
        idleSkinIndex = index;
    }


    public override void Attack()
    {
        if (isDead)    return;
        if (isDashing) return;          // Khóa tấn công khi lướt
        if (currentGun == null) return;

        // ── Guard ──
        if (!currentGun.CanFire())
        {
            // Hết đạn + chưa reload → bấm attack lần này mới bắt đầu nạp
            if (currentGun.GetCurrentAmmo() == 0 && !currentGun.IsReloading())
            {
                currentGun.Reload();
                soldierAnimator?.PlayReload();
            }
            return;
        }

        // Tính hướng bắn TRƯỚC khi trigger animation
        Transform nearestEnemy = FindNearestEnemy();
        Vector2 fireDirection;

        if (nearestEnemy != null)
        {
            // Dùng transform.position của soldier thay vì GetBulletSpawnPosition()
            // vì lúc này súng chưa vào đúng vị trí attack
            Vector3 originPos = transform.position;
            Vector3 enemyPos  = nearestEnemy.position;
            fireDirection     = (enemyPos - originPos).normalized;
            lastDirection     = fireDirection;
        }
        else
        {
            fireDirection = new Vector2(horizontal, vertical).normalized;
            if (fireDirection == Vector2.zero)
                fireDirection = lastDirection;
            else
                lastDirection = fireDirection;
        }

        pendingFireDirection = fireDirection;
        currentGun.AimAt(fireDirection);

        // Quay mặt về hướng địch trước khi bắn
        FlipToward(fireDirection);

        // Trigger animation attack — animation sẽ đưa súng lên đúng vị trí
        soldierAnimator?.PlayAttack();

        // Nếu fireDelay > 0: chờ animation đưa súng lên rồi mới bắn
        // Nếu fireDelay = 0: bạn đang dùng Animation Event (OnFireEvent) thay thế
        if (fireDelay > 0f)
            StartCoroutine(DelayedFire(fireDirection, fireDelay));
    }

    /// <summary>
    /// Gọi bởi Animation Event tại frame súng đã lên đúng vị trí bắn.
    /// Kéo event vào clip Attack, đặt tại keyframe mà mũi súng đã đến vị trí đúng.
    /// (Nhớ đặt fireDelay = 0 trong Inspector khi dùng cách này)
    /// </summary>
    public void OnFireEvent()
    {
        if (currentGun == null) return;
        currentGun.Fire(pendingFireDirection);
    }

    private IEnumerator DelayedFire(Vector2 direction, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentGun != null)
            currentGun.Fire(direction);
    }

    /// <summary>
    /// Tìm enemy gần nhất bằng tag "Enemy" — hỗ trợ mọi loại enemy
    /// </summary>
    private Transform FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return null;

        Transform nearest     = enemies[0].transform;
        float nearestDist     = Vector2.Distance(transform.position, nearest.position);

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest     = enemy.transform;
            }
        }
        return nearest;
    }

    protected override void Die()
    {
        if (hp <= 0 && !isDead)
        {
            isDead = true;
            Debug.Log("Player đã chết");

            // Dừng ngay chuyển động
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;

            // Tắt collider để zombie không tiếp tục gây sát thương
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            // Play animation & sound
            soldierAnimator?.PlayDie();
            if (dieSound != null && audioSource != null)
                audioSource.PlayOneShot(dieSound);

            // Destroy sau khi animation chết chạy xong
            Destroy(gameObject, dieDestroyDelay);
        }
    }

    protected override void Move()
    {
        if (isDead) { rb.velocity = Vector2.zero; return; }

        // Quan trọng: Gọi hàm DecayKnockback() của class cha để lực hất văng giảm dần
        DecayKnockback();

        // Nếu đang lướt thì ưu tiên lấy velocity của Dash (vẫn cộng dồn knockback nếu có)
        if (isDashing)
        {
            rb.velocity = dashDirection * dashSpeed + knockbackVelocity;
            return;
        }

        // Decay giật lùi về 0 theo thời gian
        recoilVelocity = Vector2.MoveTowards(recoilVelocity, Vector2.zero, recoilDecay * Time.deltaTime);

        Vector2 moveDirection = new Vector2(horizontal, vertical).normalized;
        
        // Cộng tổng tất cả các lực: Di chuyển cơ bản + Giật lùi của súng + Lực hất văng của Boss
        rb.velocity = moveDirection * speed + recoilVelocity + knockbackVelocity;
    }

    /// <summary>Nhận xung lực giật lùi từ súng</summary>
    public void AddRecoilImpulse(Vector2 impulse)
    {
        recoilVelocity += impulse;
    }

    public void InputMove(float inputHorizontal, float inputVertical)
    {
        horizontal = inputHorizontal;
        vertical   = inputVertical;

        // Lưu lại hướng quay mặt cuối cùng khi có di chuyển
        Vector2 inputDir = new Vector2(horizontal, vertical).normalized;
        if (inputDir != Vector2.zero)
        {
            lastDirection = inputDir;
        }

        // Lật nhân vật theo hướng ngang
        if (horizontal != 0f)
            FlipToward(new Vector2(horizontal, 0f));
    }

    // ──── Switch Weapon ────
    /// <summary>Chuyển sang súng tiếp theo (E / Scroll lên)</summary>
    public void SwitchToNextWeapon()
    {
        if (weaponPrefabs == null || weaponPrefabs.Length <= 1) return;
        currentWeaponIndex = (currentWeaponIndex + 1) % weaponPrefabs.Length;
        if (switchGunSound != null && audioSource != null)
            audioSource.PlayOneShot(switchGunSound);
        EquipWeapon(currentWeaponIndex);
    }

    /// <summary>Chuyển sang súng trước đó (Q / Scroll xuống)</summary>
    public void SwitchToPreviousWeapon()
    {
        if (weaponPrefabs == null || weaponPrefabs.Length <= 1) return;
        currentWeaponIndex = (currentWeaponIndex - 1 + weaponPrefabs.Length) % weaponPrefabs.Length;
        EquipWeapon(currentWeaponIndex);
    }

    /// <summary>Nạp đạn súng hiện tại (R)</summary>
    public void ReloadCurrentWeapon()
    {
        if (currentGun != null)
        {
            currentGun.Reload();
            soldierAnimator?.PlayReload();
            Debug.Log($"⚡ Nạp đạn: {GetCurrentWeaponName()}");
        }
    }

    public int    GetCurrentAmmo()       => currentGun != null ? currentGun.GetCurrentAmmo() : 0;
    public int    GetMaxAmmo()           => currentGun != null ? currentGun.GetMaxAmmo() : 0;
    public string GetCurrentWeaponName() => currentGun != null ? currentGun.gameObject.name  : "No Weapon";
    
    public Sprite GetCurrentWeaponSprite()
    {
        if (currentGun != null)
        {
            SpriteRenderer sr = currentGun.GetComponent<SpriteRenderer>();
            if (sr != null) return sr.sprite;
        }
        return null;
    }
    
    public float GetGunCooldown()    => currentGun != null ? currentGun.GetCurrentCooldown() : 0f;
    public float GetGunMaxCooldown() => currentGun != null ? currentGun.GetMaxCooldown() : 1f;
    public float  GetBoomCooldown()      => Mathf.Max(0, boomCooldownTimer);
    public float  GetBoomCooldownMax()   => boomCooldown;
    public bool   IsBoomReady()          => boomCooldownTimer <= 0;

    public float  GetDashCooldown()      => Mathf.Max(0, dashCooldownTimer);
    public float  GetDashCooldownMax()   => dashCooldown;
    public bool   IsDashReady()          => dashCooldownTimer <= 0;

    // ──── Boom ────
    /// <summary>Ném lựu đạn về hướng địch gần nhất hoặc hướng cuối</summary>
    public void ThrowBoom()
    {
        if (isDead)    return;
        if (isDashing) return; // Khóa ném bom khi lướt

        if (boomPrefab == null)
        {
            Debug.LogWarning("Chưa gán boomPrefab!");
            return;
        }
        if (boomCooldownTimer > 0)
        {
            Debug.Log($"Boom đang hồi! Chờ thêm {boomCooldownTimer:F1}s");
            return;
        }

        // Xác định vị trí đích ném
        Vector2 targetPos;
        Transform nearestEnemy = FindNearestEnemy();
        Vector3 origin = throwPoint != null ? throwPoint.position : transform.position;

        if (nearestEnemy != null)
        {
            targetPos = nearestEnemy.position;
        }
        else
        {
            // Ném về phía trước theo hướng lastDirection khoảng 5 units
            targetPos = (Vector2)origin + lastDirection.normalized * 5f;
        }

        // Quay mặt về hướng ném + trigger animation
        Vector2 throwDir = ((Vector2)targetPos - (Vector2)origin).normalized;
        FlipToward(throwDir);
        soldierAnimator?.PlayThrowBoom();

        // Spawn boom
        GameObject boomObj = Instantiate(boomPrefab, origin, Quaternion.identity);
        if (boomSound != null && audioSource != null)
            audioSource.PlayOneShot(boomSound);
        Boom boom = boomObj.GetComponent<Boom>();
        if (boom != null)
            boom.Throw(targetPos, attack);

        boomCooldownTimer = boomCooldown;
        Debug.Log($"💣 Ném boom! Bắt đầu đếm ngược hồi chiêu {boomCooldown}s");
    }

    // ──── Dash ────
    public void Dash()
    {
        if (isDead) return;
        if (dashCooldownTimer > 0 || isDashing)
        {
            Debug.Log($"Dash đang hồi! Chờ thêm {dashCooldownTimer:F1}s");
            return;
        }

        Vector2 moveDir = new Vector2(horizontal, vertical).normalized;
        // Nếu đang đứng yên thì dash theo hướng đang quay mặt (lastDirection)
        dashDirection = (moveDir != Vector2.zero) ? moveDir : lastDirection;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        if (dashSound != null && audioSource != null)
            audioSource.PlayOneShot(dashSound);

        // Bật vệt lướt cũ và bắt đầu vệt dự ảnh (Ghost Trail) mới
        if (dashTrail != null) dashTrail.emitting = true;
        StartCoroutine(SpawnGhostTrailRoutine());

        Debug.Log("💨 Lướt!");
    }
    
    // ──── Ghost Trail Effect ────
    private IEnumerator SpawnGhostTrailRoutine()
    {
        float spawnInterval = 0.05f; // Cứ 0.05s tạo 1 dự ảnh
        while (isDashing)
        {
            CreateGhost();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void CreateGhost()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) return;

        foreach (SpriteRenderer mySr in spriteRenderers)
        {
            if (mySr == null || mySr.sprite == null || !mySr.enabled) continue;

            GameObject ghostObj = new GameObject("DashGhost");
            ghostObj.transform.position = mySr.transform.position;
            ghostObj.transform.rotation = mySr.transform.rotation;
            ghostObj.transform.localScale = mySr.transform.lossyScale; // Áp dụng tỷ lệ chính xác từ cha

            SpriteRenderer sr = ghostObj.AddComponent<SpriteRenderer>();
            sr.sprite = mySr.sprite;
            sr.sortingLayerName = mySr.sortingLayerName;
            sr.sortingOrder = mySr.sortingOrder - 1; // Cho chìm dưới bản gốc
            sr.color = Color.black; // Xanh dương ngọc (Cyan) bán trong suốt

            StartCoroutine(FadeGhost(sr, 0.3f)); // Mờ dần trong 0.3s
        }
    }

    private IEnumerator FadeGhost(SpriteRenderer sr, float fadeTime)
    {
        float t = 0;
        Color startColor = sr.color;
        while (t < fadeTime)
        {
            if (sr == null) yield break;
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, t / fadeTime);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
    }
}

