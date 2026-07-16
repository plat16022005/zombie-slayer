using UnityEngine;

public abstract class ExpGem : MonoBehaviour
{
    [Tooltip("Tốc độ bay về phía người chơi (sẽ tăng dần)")]
    public float moveSpeed = 5f;

    private Transform targetPlayer;
    private bool isFlying = false;

    protected virtual void Start()
    {
        // Tránh tình trạng quá nhiều ngọc làm lag game, tự hủy sau 60 giây nếu không nhặt
        Destroy(gameObject, 60f);
    }

    // Hàm này sẽ được gọi bởi MagnetField (Vòng nam châm của người chơi)
    public void FlyTo(Transform playerBody)
    {
        if (!isFlying)
        {
            targetPlayer = playerBody;
            isFlying = true;
        }
    }

    protected virtual void Update()
    {
        // Chỉ chạy logic di chuyển nếu đã bị nam châm hút
        if (isFlying && targetPlayer != null)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPlayer.position, moveSpeed * Time.deltaTime);
            moveSpeed += 15f * Time.deltaTime; // Gia tốc bay nhanh dần để tạo cảm giác hút mạnh
        }
    }

    // Yêu cầu các script con phải tự định nghĩa lượng Exp của nó
    public abstract int GetExpAmount();

    // Khi người chơi chạm vào viên ngọc (Lưu ý: Gem phải có Collider2D tick IsTrigger)
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Soldier soldier = collision.GetComponent<Soldier>();
            if (soldier != null)
            {
                soldier.AddExp(GetExpAmount());
                Destroy(gameObject);
            }
        }
    }
}
