using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class RunSoundButton : MonoBehaviour
{
    [Header("Sound Settings")]
    [Tooltip("Kéo thả file âm thanh (AudioClip) bạn muốn phát khi bấm nút vào đây")]
    public AudioClip clickSound;

    private Button button;
    private AudioSource audioSource;

    private void Awake()
    {
        // Lấy component Button gắn trên cùng GameObject
        button = GetComponent<Button>();

        // Tạo một AudioSource ẩn để phát âm thanh (nếu chưa có)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Lắng nghe sự kiện click của Button
        if (button != null)
        {
            button.onClick.AddListener(PlaySound);
        }
    }

    private void PlaySound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    private void OnDestroy()
    {
        // Gỡ bỏ lắng nghe sự kiện để tránh lỗi memory leak khi đổi Scene
        if (button != null)
        {
            button.onClick.RemoveListener(PlaySound);
        }
    }
}
