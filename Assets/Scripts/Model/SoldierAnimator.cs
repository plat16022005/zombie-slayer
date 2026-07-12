using UnityEngine;

/// <summary>
/// Quản lý toàn bộ animation của Soldier.
/// Setup Animator:
///   - 1 Animator duy nhất với nhiều Layer (Head / Body / Legs)
///   - Body Idle dùng Blend Tree kiểm soát bởi float "IdleType" (0.0 / 1.0 / 2.0)
/// </summary>
public class SoldierAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    // ──── Tên Parameter trong Animator (phải khớp chính xác) ────

    // Float — Blend Tree chọn loại Idle (0 = Idle1, 1 = Idle2, 2 = Idle3)
    private const string PARAM_IDLE_TYPE   = "TypeGun";

    // Bool
    private const string PARAM_IS_MOVING   = "isMoving";
    private const string PARAM_IS_DASHING  = "isDashing";

    // Trigger
    private const string PARAM_ATTACK      = "Attack";
    private const string PARAM_DIE         = "Die";
    private const string PARAM_RELOAD      = "Reload";
    private const string PARAM_HIT         = "Hit";
    private const string PARAM_THROW_BOOM  = "ThrowBoom";

    // ──── Khởi tạo ────

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Gọi từ Soldier.Start() để chọn loại Idle theo skin/nhân vật.
    /// idleIndex: 0 = Idle1 / 1 = Idle2 / 2 = Idle3
    /// </summary>
    public void Init(int idleIndex)
    {
        // Blend Tree dùng float — ta set đúng giá trị 0 / 1 / 2
        animator.SetFloat(PARAM_IDLE_TYPE, idleIndex);
        Debug.Log($"[SoldierAnimator] Init IdleType = {idleIndex}");
    }

    // ──── Cập nhật mỗi frame ────

    /// <summary>
    /// Gọi từ Soldier.Update() mỗi frame để cập nhật animation di chuyển / dash.
    /// </summary>
    public void UpdateState(Vector2 moveVelocity, bool isDashing, float idleType)
    {
        bool isMoving = moveVelocity.sqrMagnitude > 0.01f;
        animator.SetFloat(PARAM_IDLE_TYPE, idleType);
        animator.SetBool(PARAM_IS_MOVING,  isMoving);
        animator.SetBool(PARAM_IS_DASHING, isDashing);
    }

    // ──── One-shot Triggers ────

    public void PlayAttack()    => animator.SetTrigger(PARAM_ATTACK);
    public void PlayDie()       => animator.SetTrigger(PARAM_DIE);
    public void PlayReload()    => animator.SetTrigger(PARAM_RELOAD);
    public void PlayHit()       => animator.SetTrigger(PARAM_HIT);
    public void PlayThrowBoom() => animator.SetTrigger(PARAM_THROW_BOOM);
}
