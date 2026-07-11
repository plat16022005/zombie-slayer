
using UnityEngine;

public class Soldier : Character
{
    private float horizontal;
    private float vertical;
    private Vector2 lastDirection = Vector2.right;
    // ──── Recoil ────
    private Vector2 recoilVelocity = Vector2.zero;
    [SerializeField] private float recoilDecay = 20f;  // Tốc độ tắt dần giật lùi


    // ──── Weapon System ────
    [Header("Weapon System")]
    [SerializeField] private GameObject[] weaponPrefabs;   // Danh sách prefab súng
    [SerializeField] private Transform weaponHolder;        // Điểm gắn súng lên người
    [SerializeField] private AudioClip switchGunSound;

    private Gun currentGun;         // Instance súng đang dùng
    private int currentWeaponIndex = 0;

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
        hp = 100;
        attack = 20;
        speed = 10;
        defend = 5;
    }

    protected override void Awake()
    {
        base.Awake();
        EquipWeapon(currentWeaponIndex);
        boomCooldownTimer = 0f; // Sẵn sàng ném ngay từ đầu
        dashCooldownTimer = 0f; // Sẵn sàng lướt
        if (dashTrail != null) dashTrail.emitting = false;
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
    }
    public override void TakeDame(float dame)
    {
        base.TakeDame(dame);
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
    }


    public override void Attack()
    {
        if (isDashing) return; // Khóa tấn công khi lướt
        if (currentGun == null) return;

        // Tìm enemy gần nhất theo tag
        Transform nearestEnemy = FindNearestEnemy();
        Vector2 fireDirection;

        if (nearestEnemy != null)
        {
            Vector3 gunPos    = currentGun.GetBulletSpawnPosition();
            Vector3 enemyPos  = nearestEnemy.position;
            fireDirection     = (enemyPos - gunPos).normalized;
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

        currentGun.AimAt(fireDirection);
        currentGun.Fire(fireDirection);
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
        if (hp <= 0)
        {
            Debug.Log("Player đã chết");
            Destroy(this.gameObject);
        }
    }

    protected override void Move()
    {
        // Nếu đang lướt thì ưu tiên lấy velocity của Dash
        if (isDashing)
        {
            rb.velocity = dashDirection * dashSpeed;
            return;
        }

        // Decay giật lùi về 0 theo thời gian
        recoilVelocity = Vector2.MoveTowards(recoilVelocity, Vector2.zero, recoilDecay * Time.deltaTime);

        Vector2 moveDirection = new Vector2(horizontal, vertical).normalized;
        rb.velocity = moveDirection * speed + recoilVelocity;
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
            Debug.Log($"⚡ Nạp đạn: {GetCurrentWeaponName()}");
        }
    }

    public int    GetCurrentAmmo()       => currentGun != null ? currentGun.GetCurrentAmmo() : 0;
    public string GetCurrentWeaponName() => currentGun != null ? currentGun.gameObject.name  : "No Weapon";
    public float  GetBoomCooldown()      => Mathf.Max(0, boomCooldownTimer);
    public bool   IsBoomReady()          => boomCooldownTimer <= 0;

    // ──── Boom ────
    /// <summary>Ném lựu đạn về hướng địch gần nhất hoặc hướng cuối</summary>
    public void ThrowBoom()
    {
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

        // Spawn boom
        GameObject boomObj = Instantiate(boomPrefab, origin, Quaternion.identity);
        if (boomSound != null && audioSource != null)
            audioSource.PlayOneShot(boomSound);
        Boom boom = boomObj.GetComponent<Boom>();
        if (boom != null)
            boom.Throw(targetPos);

        boomCooldownTimer = boomCooldown;
        Debug.Log($"💣 Ném boom! Bắt đầu đếm ngược hồi chiêu {boomCooldown}s");
    }

    // ──── Dash ────
    public void Dash()
    {
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

        // Bật vệt lướt
        if (dashTrail != null) dashTrail.emitting = true;

        Debug.Log("💨 Lướt!");
    }
}

