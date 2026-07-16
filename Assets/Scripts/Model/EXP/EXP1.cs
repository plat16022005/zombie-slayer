using UnityEngine;

public class EXP1 : ExpGem
{
    [Tooltip("Lượng kinh nghiệm viên ngọc này cung cấp")]
    public int expValue = 10;

    public override int GetExpAmount()
    {
        return expValue;
    }
}
