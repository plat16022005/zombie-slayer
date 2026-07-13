using UnityEngine;

public class ZombieBossAnimator : EnemyAnimator
{
    // ──── Tên Parameter (phải khớp chính xác trong Animator) ────
    private const string PARAM_SKILL_CHARGE = "Skill_Charge";
    private const string PARAM_SKILL_SUMMON = "Skill_Summon";
    private const string PARAM_SKILL_JUMP   = "Skill_Jump";

    // ──── Custom Triggers cho Boss ────
    public void PlayCharge() => animator.SetTrigger(PARAM_SKILL_CHARGE);
    public void PlaySummon() => animator.SetTrigger(PARAM_SKILL_SUMMON);
    public void PlayJump()   => animator.SetTrigger(PARAM_SKILL_JUMP);
    
    public override void PlayDie()
    {
        animator.ResetTrigger(PARAM_SKILL_CHARGE);
        animator.ResetTrigger(PARAM_SKILL_SUMMON);
        animator.ResetTrigger(PARAM_SKILL_JUMP);
        animator.ResetTrigger("Hit");
        animator.ResetTrigger("Attack");

        animator.Play("Body_Dead", 0, 0f); 

        animator.Play("Head_Dead", 1, 0f);
        animator.Play("Legs_Dead", 2, 0f);
    }
}
