using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "New Exp Database", menuName = "Zombie Slayer/Exp Database")]
public class ExpDatabase : ScriptableObject
{
    [Tooltip("Kéo thả các Prefab Ngọc Kinh Nghiệm (Đã gắn Script kế thừa ExpGem) vào đây. Hệ thống sẽ tự động đọc lượng Exp bên trong chúng để sắp xếp tối ưu.")]
    public List<GameObject> expGemPrefabs;

    public void SpawnExpGems(int totalExp, Vector3 spawnPosition)
    {
        if (expGemPrefabs == null || expGemPrefabs.Count == 0) return;

        // Đọc giá trị Exp từ các Prefab
        List<KeyValuePair<int, GameObject>> gemList = new List<KeyValuePair<int, GameObject>>();

        foreach (var prefab in expGemPrefabs)
        {
            if (prefab == null) continue;
            
            ExpGem gemScript = prefab.GetComponent<ExpGem>();
            if (gemScript != null)
            {
                // Gọi hàm GetExpAmount() từ Script con của Prefab để biết viên ngọc này cho bao nhiêu Exp
                int expVal = gemScript.GetExpAmount();
                if (expVal > 0)
                {
                    gemList.Add(new KeyValuePair<int, GameObject>(expVal, prefab));
                }
            }
        }

        // Sắp xếp ngọc từ LỚN NHẤT đến NHỎ NHẤT (Tham lam - Greedy Algorithm)
        var sortedGems = gemList.OrderByDescending(g => g.Key).ToList();

        int remainingExp = totalExp;

        foreach (var gem in sortedGems)
        {
            int expVal = gem.Key;
            GameObject prefab = gem.Value;

            int count = remainingExp / expVal; // Lấy số lượng ngọc loại bự này có thể rơi ra
            
            for (int i = 0; i < count; i++)
            {
                // Cho ngọc văng ngẫu nhiên xung quanh 1 xíu để chúng không xếp đè khít lên nhau
                Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * 0.8f;
                Vector3 finalPos = new Vector3(spawnPosition.x + randomOffset.x, spawnPosition.y + randomOffset.y, spawnPosition.z);
                
                GameObject spawnedGem = Instantiate(prefab, finalPos, Quaternion.identity);
                // Ép buộc gán lại vị trí một lần nữa để đảm bảo không bị lỗi rớt ở (0,0)
                spawnedGem.transform.position = finalPos;
            }

            remainingExp %= expVal; // Lấy phần dư EXP còn lại để tiếp tục chia cho các viên ngọc nhỏ hơn
            
            if (remainingExp <= 0) break; // Đã rớt đủ Exp thì dừng
        }
    }
}
