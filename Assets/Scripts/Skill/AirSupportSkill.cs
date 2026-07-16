using UnityEngine;

public class AirSupportSkill : MonoBehaviour
{
    [Header("Skill Settings")]
    [Tooltip("Kéo thả Prefab Máy Bay (đã gắn script AttackPlane) vào đây")]
    public GameObject planePrefab;
    [Tooltip("Khoảng thời gian gọi máy bay một lần (giây)")]
    public float cooldownDuration = 10f;
    [Tooltip("Khoảng cách máy bay xuất hiện đằng sau lưng Player")]
    public float spawnDistance = 15f; 
    [Tooltip("Độ cao của máy bay so với mặt đất (Z/Y tuỳ góc nhìn)")]
    public float flyingHeight = 5f;

    [Header("Level Settings")]
    public int currentLevel = 1;
    public int maxLevel = 5;

    private Transform player;
    private float timer;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Gọi luôn một đợt máy bay khi vừa nhặt được skill
        timer = 0f; 
    }

    void Update()
    {
        if (player == null) return;

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            SummonPlane();
            timer = cooldownDuration;
        }
    }

    private void SummonPlane()
    {
        if (planePrefab == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // Tính toạ độ 2 cạnh màn hình trong World Space (Z=nearClipPlane)
        // x = 1.2 là cách mép phải một chút, x = -0.2 là cách mép trái một chút. y = 0.8 là bay tít trên cao
        Vector3 rightEdge = cam.ViewportToWorldPoint(new Vector3(1.2f, 0.8f, cam.nearClipPlane));
        Vector3 leftEdge = cam.ViewportToWorldPoint(new Vector3(-0.2f, 0.8f, cam.nearClipPlane));

        // Sinh máy bay ở bên Phải màn hình và GIỮ NGUYÊN góc quay gốc của Prefab
        Vector3 spawnPos = new Vector3(rightEdge.x, rightEdge.y, 0f);
        GameObject plane = Instantiate(planePrefab, spawnPos, planePrefab.transform.rotation);
        
        // Không xoay mặt sprite nữa theo yêu cầu của bạn
        // plane.transform.rotation = Quaternion.Euler(0, 0, 180f);

        AttackPlane script = plane.GetComponent<AttackPlane>();
        if (script != null)
        {
            // Quãng đường bay ngang qua điện thoại
            float distanceToFly = Mathf.Abs(rightEdge.x - leftEdge.x);
            
            // Công thức vật lý cơ bản: Vận Tốc = Quãng Đường / Thời Gian (5 giây)
            script.flySpeed = distanceToFly / 5f;
            
            script.UpdateStats(currentLevel);
        }
    }

    public void LevelUp()
    {
        if (currentLevel >= maxLevel) return;
        currentLevel++;
        
        // Mỗi lần tăng cấp, máy bay được gọi ra nhanh hơn
        cooldownDuration = Mathf.Max(5f, cooldownDuration - 1f);
    }
}
