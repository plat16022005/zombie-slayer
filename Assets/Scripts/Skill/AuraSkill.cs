using UnityEngine;

public class AuraSkill : MonoBehaviour
{
    [Header("Combat Settings")]
    [Tooltip("Bán kính vùng sát thương")]
    public float auraRadius = 3f;
    [Tooltip("Hệ số sát thương nhân với sức mạnh tấn công gốc của Player")]
    public float damageMultiplier = 0.5f;
    [Tooltip("Thời gian chờ giữa các lần giật máu (giây)")]
    public float damageTickRate = 0.5f;

    [Header("Cooldown Settings")]
    [Tooltip("Thời gian Aura xuất hiện (giây)")]
    public float activeDuration = 5f;
    [Tooltip("Thời gian Aura tắt để hồi chiêu (giây)")]
    public float cooldownDuration = 5f;

    [Header("Visual Settings")]
    [Tooltip("Kéo thả Prefab Particle System (Aura) vào đây")]
    public ParticleSystem auraParticlePrefab;
    private ParticleSystem activeAuraParticle;

    [Header("Level Settings")]
    public int currentLevel = 1;
    public int maxLevel = 5;

    private Transform player;
    private Soldier playerSoldier;
    private float tickTimer;

    // Cooldown state
    private bool isAuraActive = true;
    private float stateTimer = 0f;

    void Start()
    {
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player = pObj.transform;
            playerSoldier = pObj.GetComponent<Soldier>();
        }

        // Sinh ra hiệu ứng Particle Aura
        if (auraParticlePrefab != null)
        {
            activeAuraParticle = Instantiate(auraParticlePrefab, transform.position, Quaternion.identity);
            activeAuraParticle.transform.SetParent(transform); // Nối vào script này
            
            // Ép Z = -0.5f để đè lên Background luôn lúc khởi tạo
            activeAuraParticle.transform.localPosition = new Vector3(0, 0, -0.5f);
        }
        
        // Cập nhật lại độ lớn của hình ảnh cho khớp với thông số
        UpdateVisualScale();
    }

    void Update()
    {
        if (player == null) return;

        // Luôn bám dính lấy người chơi, ép Z = -0.5f để vòng tròn luôn nổi lên trên Background
        transform.position = new Vector3(player.position.x, player.position.y, -0.5f);

        stateTimer += Time.deltaTime;

        if (isAuraActive)
        {
            if (stateTimer >= activeDuration)
            {
                isAuraActive = false;
                stateTimer = 0f;
                SetAuraActive(false);
            }
            else
            {
                // Bộ đếm thời gian giật sát thương
                tickTimer -= Time.deltaTime;
                if (tickTimer <= 0)
                {
                    DealAreaDamage();
                    tickTimer = damageTickRate;
                }
            }
        }
        else
        {
            if (stateTimer >= cooldownDuration)
            {
                isAuraActive = true;
                stateTimer = 0f;
                SetAuraActive(true);
            }
        }
    }

    private void SetAuraActive(bool active)
    {
        if (activeAuraParticle != null)
        {
            activeAuraParticle.gameObject.SetActive(active);
        }
    }

    private void DealAreaDamage()
    {
        // Quét tất cả quái vật nằm trong vùng bán kính
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, auraRadius);
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null && enemy.hp > 0)
                {
                    // Tính sát thương
                    float damage = playerSoldier != null ? playerSoldier.attack * damageMultiplier : 10f * damageMultiplier;
                    
                    enemy.TakeDame(damage);
                }
            }
        }
    }

    public void LevelUp()
    {
        if (currentLevel >= maxLevel) return;
        currentLevel++;

        switch (currentLevel)
        {
            case 2:
                damageMultiplier = 0.8f; // Tăng lượng sát thương
                break;
            case 3:
                auraRadius += 1.5f; // Vòng aura bự ra thêm
                UpdateVisualScale();
                break;
            case 4:
                damageTickRate = 0.3f; // Giật máu nhanh hơn (từ 0.5s xuống 0.3s)
                damageMultiplier = 1.0f; 
                break;
            case 5:
                auraRadius += 1.5f; // Aura khổng lồ
                UpdateVisualScale();
                damageMultiplier = 1.5f; // Sát thương khủng
                break;
        }

        cooldownDuration = Mathf.Max(1f, cooldownDuration - 1f);
    }

    private void UpdateVisualScale()
    {
        // Tự động thay đổi Bán kính phát hạt của Particle System cho khớp với auraRadius
        if (activeAuraParticle != null)
        {
            // Can thiệp thẳng vào Module Shape (Nơi nhả hạt) để ép bán kính của nó bằng chính xác auraRadius
            var shape = activeAuraParticle.shape;
            if (shape.enabled)
            {
                shape.radius = auraRadius;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Vẽ vòng tròn đỏ rỗng trong Scene để bạn dễ hình dung vùng sát thương
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, auraRadius);
    }
}
