using UnityEngine;

public abstract class Item : MonoBehaviour
{
    [Tooltip("Tốc độ bay về phía người chơi (sẽ tăng dần)")]
    public float moveSpeed = 5f;
    private Transform targetPlayer;
    private bool isFlying = false;

    protected virtual void Start()
    {
        Destroy(gameObject, 60f);
    }

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
        if (isFlying && targetPlayer != null)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPlayer.position, moveSpeed * Time.deltaTime);
            moveSpeed += 15f * Time.deltaTime;
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Soldier soldier = collision.GetComponent<Soldier>();
            if (soldier != null)
            {
                OnCollect(soldier);
                Destroy(gameObject);
            }
        }
    }

    protected abstract void OnCollect(Soldier player);
}
