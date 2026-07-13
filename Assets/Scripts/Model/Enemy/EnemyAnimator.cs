using UnityEngine;

/// <summary>
/// Quản lý toàn bộ animation của Enemy.
/// Gắn vào cùng GameObject hoặc parent của Animator.
///
/// Tham số Animator cần tạo:
///   Bool    : isMoving
///   Trigger : Attack, Hit, Die
/// </summary>
public class EnemyAnimator : MonoBehaviour
{
    [SerializeField] protected Animator animator;

    // ──── Tên Parameter (phải khớp chính xác trong Animator) ────
    private const string PARAM_IS_MOVING = "isMoving";
    private const string PARAM_ATTACK    = "Attack";
    private const string PARAM_HIT       = "Hit";
    private const string PARAM_DIE       = "Die";
    private const string PARAM_SPAWN     = "Spawn";

    protected virtual void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    // ──── Cập nhật mỗi frame ────

    /// <summary>
    /// Gọi từ Enemy.Update() mỗi frame.
    /// isMoving = true khi zombie đang đuổi player.
    /// </summary>
    public void UpdateState(bool isMoving)
    {
        animator.SetBool(PARAM_IS_MOVING, isMoving);
    }

    // ──── One-shot Triggers ────
    public virtual void PlayAttack() => animator.SetTrigger(PARAM_ATTACK);
    public virtual void PlayHit()    => animator.SetTrigger(PARAM_HIT);
    public virtual void PlayDie()    => animator.SetTrigger(PARAM_DIE);
    public virtual void PlaySpawn()  => animator.SetTrigger(PARAM_SPAWN);
}
