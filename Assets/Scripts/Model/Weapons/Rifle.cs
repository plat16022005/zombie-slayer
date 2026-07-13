using UnityEngine;

/// <summary>
/// Súng trường: Bắn chậm, sát thương cao
/// </summary>
public class Rifle : Gun
{
    [SerializeField] private GameObject bulletPrefab;
    private float damage = 30f;

    protected override void Init()
    {
        fireRate    = 0.15f;
        maxAmmo     = 30;
        reloadTime  = 2f;
        recoilForce = 6f;
    }

    public override void Fire(Vector2 direction)
    {
        if (!CanFire())
            return;

        base.Fire(direction);

        // Tạo viên đạn từ mũi súng
        if (bulletPrefab != null && bulletSpawnPoint != null)
        {
            GameObject bulletObj = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.identity);
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.Init(direction);
            }
            Debug.Log($"Rifle bắn! Sát thương cao: {damage}");
        }
    }
}
