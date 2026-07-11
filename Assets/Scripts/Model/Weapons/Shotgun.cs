using UnityEngine;

/// <summary>
/// Súng Shotgun: Bắn 5 viên cùng lúc theo hình quạt, sát thương thấp mỗi viên
/// </summary>
public class Shotgun : Gun
{
    [SerializeField] private GameObject bulletPrefab;

    [Header("Shotgun Settings")]
    [SerializeField] private int pelletCount = 5;       // Số viên đạn mỗi lần bắn
    [SerializeField] private float spreadAngle = 40f;   // Tổng góc quạt (độ)
    private float damage = 10f;

    protected override void Init()
    {
        fireRate    = 1.2f;
        maxAmmo     = 15;
        reloadTime  = 2f;
        recoilForce = 12f;
    }

    // Mỗi phát bắn tiêu hao đúng số viên đạn (= pelletCount)
    protected override int AmmoCostPerShot => pelletCount;


    public override void Fire(Vector2 direction)
    {
        if (!CanFire())
            return;

        base.Fire(direction);

        if (bulletPrefab == null || bulletSpawnPoint == null)
            return;

        // Tính góc bước giữa các viên đạn
        // Ví dụ: 5 viên, spread 40° → -20°, -10°, 0°, +10°, +20°
        float startAngle = -spreadAngle / 2f;
        float step       = pelletCount > 1 ? spreadAngle / (pelletCount - 1) : 0f;

        for (int i = 0; i < pelletCount; i++)
        {
            float   offsetDeg  = startAngle + step * i;
            Vector2 pelletDir  = RotateVector(direction, offsetDeg);

            GameObject bulletObj = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.identity);
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.Init(pelletDir);
            }
        }

        Debug.Log($"Shotgun bắn! {pelletCount} viên, spread {spreadAngle}°, sát thương mỗi viên: {damage}");
    }

    /// <summary>
    /// Xoay vector 2D một góc degrees
    /// </summary>
    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }
}
