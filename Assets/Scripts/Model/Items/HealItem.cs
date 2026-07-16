using UnityEngine;

public class HealItem : Item
{
    [Tooltip("Phần trăm máu tối đa được hồi (0.3 = 30%)")]
    public float healPercentage = 0.3f;

    protected override void OnCollect(Soldier player)
    {
        player.Heal(healPercentage, true);
        Debug.Log($"Đã nhặt được Item Hồi máu! ({healPercentage * 100}% máu tối đa)");
    }
}
