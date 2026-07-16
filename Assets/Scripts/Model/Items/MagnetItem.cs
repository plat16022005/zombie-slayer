using UnityEngine;

public class MagnetItem : Item
{
    protected override void OnCollect(Soldier player)
    {
        // Hút tất cả các viên ngọc Exp trên màn hình
        ExpGem[] allGems = FindObjectsOfType<ExpGem>();
        foreach (ExpGem gem in allGems)
        {
            gem.FlyTo(player.transform);
        }

        // Hút tất cả các item khác (trừ chính nó)
        Item[] allItems = FindObjectsOfType<Item>();
        foreach (Item item in allItems)
        {
            if (item != this)
                item.FlyTo(player.transform);
        }

        Debug.Log("Đã nhặt được Item Nam châm toàn map!");
    }
}
