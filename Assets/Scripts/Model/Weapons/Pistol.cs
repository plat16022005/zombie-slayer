using UnityEngine;

/// <summary>
/// Súng ngắn: Bắn nhanh, sát thương vừa
/// </summary>
public class Pistol : Gun
{
    [SerializeField] private GameObject bulletPrefab;
    protected override void Init()
    {
        fireRate    = 0.4f;
        maxAmmo     = 10;
        reloadTime  = 1f;
        recoilForce = 3f;
    }

    public override void Fire(Vector2 direction)
    {
        if (!CanFire())
            return;

        base.Fire(direction);

        // Tạo viên đạn từ mũi súng
        if (bulletPrefab != null && bulletSpawnPoint != null)
        {
            float totalDamage = ownerSoldier != null ? ownerSoldier.attack : 0f;
            GameObject bulletObj = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.identity);
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.Init(direction, totalDamage);
            }
            Debug.Log($"Pistol bắn! Sát thương cao: {totalDamage}");
        }
    }
}
