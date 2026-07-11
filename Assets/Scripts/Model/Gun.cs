using System.Collections;
using UnityEngine;


public abstract class Gun : MonoBehaviour
{
    [SerializeField] protected float fireRate;
    [SerializeField] protected int   maxAmmo;
    [SerializeField] protected float reloadTime;        // Thời gian nạp đạn (giây)
    [SerializeField] protected float recoilForce;       // Lực giật lùi người chơi
    [SerializeField] protected Transform bulletSpawnPoint;

    [Header("VFX")]
    [SerializeField] protected GameObject muzzleFlashPrefab; // Particle ở đầu nòng
    [SerializeField] protected float kickDistance = 0.1f;    // Khoảng giật lùi visual
    [SerializeField] protected float kickDuration = 0.08f;   // Thời gian kick (giây)

    [Header("SFX")]
    [SerializeField] protected AudioClip fireSound;          // Tiếng bắn
    [SerializeField] protected AudioClip reloadSound;        // Tiếng nạp đạn
    protected AudioSource audioSource;


    private int         currentAmmo;
    private float       fireTimer   = 0f;
    private bool        isReloading = false;
    private float       reloadTimer = 0f;
    private Soldier     ownerSoldier;                       // Soldier cầm súng

    protected abstract void Init();

    /// <summary>
    /// Số đạn tiêu hao mỗi lần bắn. Override ở súng con nếu cần.
    /// </summary>
    protected virtual int AmmoCostPerShot => 1;

    protected virtual void Awake()
    {
        Init();
        
        // Lấy hoặc tự động thêm AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
    }

    protected virtual void Start()
    {
        currentAmmo = maxAmmo;

        // Lấy Soldier từ GameObject cha
        ownerSoldier = GetComponentInParent<Soldier>();
        if (ownerSoldier == null)
            Debug.LogWarning($"{name}: Không tìm thấy Soldier trên parent!");
    }

    protected virtual void Update()
    {
        fireTimer -= Time.deltaTime;

        // Đang nạp đạn — đếm ngược
        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0f)
            {
                currentAmmo = maxAmmo;
                isReloading = false;
                Debug.Log($"✅ Nạp đạn xong! Đạn: {currentAmmo}/{maxAmmo}");
            }
        }
    }

    public virtual void Fire(Vector2 direction)
    {
        if (!CanFire())
            return;

        currentAmmo -= AmmoCostPerShot;
        fireTimer    = fireRate;

        // Giật lùi người chơi
        ApplyRecoil(direction);

        // Muzzle flash tại đầu nòng súng
        SpawnMuzzleFlash();

        // Kick animation (súng giật lùi rồi spring về)
        StartCoroutine(KickRoutine());

        // Báo hiệu GameFeelManager rung màn hình dựa theo recoilForce
        if (GameFeelManager.Instance != null)
        {
            // Cinemachine cần Amplitude lớn hơn một chút để thấy rõ.
            // Thời gian rung (Duration) chỉ nên rất ngắn (0.05 - 0.1s) để súng bắn nhanh không bị chóng mặt.
            float shakeMagnitude = Mathf.Clamp(recoilForce * 0.2f, 0.5f, 3f); 
            GameFeelManager.Instance.ShakeCamera(0.2f, shakeMagnitude);
        }

        // Phát âm thanh bắn súng
        if (fireSound != null && audioSource != null)
            audioSource.PlayOneShot(fireSound);

        Debug.Log($"Bắn! Đạn còn: {currentAmmo}/{maxAmmo}");

        // Hết đạn → tự động bắt đầu nạp
        if (currentAmmo < AmmoCostPerShot)
            Reload();
    }

    public bool CanFire()
    {
        return fireTimer <= 0 && currentAmmo >= AmmoCostPerShot && !isReloading;
    }

    /// <summary>
    /// Bắt đầu nạp đạn — phải chờ reloadTime giây mới đầy
    /// </summary>
    public void Reload()
    {
        if (isReloading || currentAmmo == maxAmmo) return;

        isReloading = true;
        reloadTimer = reloadTime;

        // Phát âm thanh nạp đạn
        if (reloadSound != null && audioSource != null)
            audioSource.PlayOneShot(reloadSound);

        Debug.Log($"🔄 Đang nạp đạn... ({reloadTime}s)");
    }

    /// <summary>
    /// Lấy vị trí mũi súng
    /// </summary>
    public Vector3 GetBulletSpawnPosition()
    {
        if (bulletSpawnPoint != null)
            return bulletSpawnPoint.position;
        return transform.position;
    }

    public int  GetCurrentAmmo()    => currentAmmo;
    public int  GetMaxAmmo()        => maxAmmo;
    public bool IsReloading()       => isReloading;
    public float GetReloadProgress() => isReloading ? 1f - (reloadTimer / reloadTime) : 1f;

    /// <summary>Xoay súng về hướng chỉ định (dùng cho 2D top-down / side-scroller)</summary>
    public void AimAt(Vector2 direction)
    {
        if (direction == Vector2.zero) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>Spawn muzzle flash tại đầu nòng</summary>
    private void SpawnMuzzleFlash()
    {
        if (muzzleFlashPrefab == null || bulletSpawnPoint == null) return;
        GameObject flash = Instantiate(muzzleFlashPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        flash.transform.SetParent(bulletSpawnPoint); // gắn theo nòng súng
        Destroy(flash, 0.05f);                       // tự xóa sau 50ms
    }

    /// <summary>Hiệu ứng súng giật lùi rồi spring về vị trí gốc</summary>
    private IEnumerator KickRoutine()
    {
        Vector3 originPos = transform.localPosition;
        Vector3 kickBack  = originPos + transform.right * (-kickDistance); // lùi theo trục của súng

        // Giật nhanh ra sau
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / (kickDuration * 0.4f);
            transform.localPosition = Vector3.Lerp(originPos, kickBack, t);
            yield return null;
        }

        // Spring trở về chầm hơn
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / (kickDuration * 0.6f);
            transform.localPosition = Vector3.Lerp(kickBack, originPos, t);
            yield return null;
        }

        transform.localPosition = originPos; // đảm bảo về đúng chính xác
    }

    /// <summary>Đẩy người cầm súng ngược chiều bắn (giật lùi)</summary>
    private void ApplyRecoil(Vector2 fireDirection)
    {
        if (ownerSoldier == null || recoilForce <= 0f) return;

        Vector2 recoilDir = -fireDirection.normalized;
        ownerSoldier.AddRecoilImpulse(recoilDir * recoilForce);
    }

    // public float GetDamage()
    // {
    //     return damage;
    // }
}
