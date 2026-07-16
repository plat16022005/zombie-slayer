using UnityEngine;

public class MagnetField : MonoBehaviour
{
    private Transform playerBody;

    void Start()
    {
        // MagnetField sẽ là con của Player, nên transform gốc của Player là parent
        if (transform.parent != null)
        {
            playerBody = transform.parent;
        }
        else
        {
            playerBody = transform;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Khi vòng nam châm chạm vào ngọc, ra lệnh cho ngọc bay vào thân người chơi
        ExpGem gem = collision.GetComponent<ExpGem>();
        if (gem != null)
        {
            gem.FlyTo(playerBody);
        }

        // Hút luôn cả vật phẩm (Item)
        Item item = collision.GetComponent<Item>();
        if (item != null)
        {
            item.FlyTo(playerBody);
        }
    }
}
