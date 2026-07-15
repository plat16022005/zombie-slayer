using UnityEngine;

public class InfiniteBackground : MonoBehaviour
{
    private Transform cam;
    private float width, height;

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            width = sr.bounds.size.x;
            height = sr.bounds.size.y;
            
            // Tự động sao chép ảnh này thành một lưới 3x3 bao bọc xung quanh
            CreateGrid(sr);
        }
    }

    private void CreateGrid(SpriteRenderer original)
    {
        // Sử dụng kích thước gốc của Sprite (chưa tính scale) để đặt localPosition chính xác
        float localWidth = original.sprite.bounds.size.x;
        float localHeight = original.sprite.bounds.size.y;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; // Bỏ qua vị trí trung tâm (bản gốc)
                
                GameObject clone = new GameObject($"Bg_Clone_{x}_{y}");
                clone.transform.SetParent(transform);
                clone.transform.localPosition = new Vector3(x * localWidth, y * localHeight, 0);
                clone.transform.localScale = Vector3.one; // Khôi phục tỷ lệ gốc 1x1 cho con

                SpriteRenderer cloneSr = clone.AddComponent<SpriteRenderer>();
                cloneSr.sprite = original.sprite;
                cloneSr.sortingLayerID = original.sortingLayerID;
                cloneSr.sortingLayerName = original.sortingLayerName;
                cloneSr.sortingOrder = original.sortingOrder;
                cloneSr.color = original.color;
            }
        }
    }

    void Update()
    {
        if (cam == null || width == 0 || height == 0) return;

        // Tính khoảng cách camera so với TÂM của lưới 3x3
        float dx = cam.position.x - transform.position.x;
        float dy = cam.position.y - transform.position.y;

        // Nếu camera đi lệch ra khỏi tấm ảnh trung tâm, dời toàn bộ khối lưới 3x3 đi theo
        if (Mathf.Abs(dx) >= width)
        {
            transform.position += new Vector3(width * Mathf.Sign(dx), 0, 0);
        }

        if (Mathf.Abs(dy) >= height)
        {
            transform.position += new Vector3(0, height * Mathf.Sign(dy), 0);
        }
    }
}
