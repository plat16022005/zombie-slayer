using UnityEngine;

/// <summary>
/// Gắn vào bất kỳ VFX/Particle GameObject nào để tự động Destroy khi xong.
/// Ưu tiên: đợi ParticleSystem kết thúc; nếu không có thì dùng fixedLifeTime.
/// </summary>
public class AutoDestroy : MonoBehaviour
{
    [Tooltip("Thời gian sống cố định (giây). Dùng khi không có ParticleSystem, hoặc muốn override.")]
    [SerializeField] private float fixedLifeTime = 2f;

    [Tooltip("Nếu true: đợi ParticleSystem tự kết thúc rồi mới xóa (bỏ qua fixedLifeTime).")]
    [SerializeField] private bool waitForParticle = true;

    private ParticleSystem ps;

    private void Start()
    {
        ps = GetComponent<ParticleSystem>();

        if (ps != null && waitForParticle)
        {
            // Dùng duration + startLifetime của particle để tính thời điểm xóa chính xác
            float totalTime = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(gameObject, totalTime);
        }
        else
        {
            Destroy(gameObject, fixedLifeTime);
        }
    }
}
