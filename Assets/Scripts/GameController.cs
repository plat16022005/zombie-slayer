using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public Soldier player;
    public Joystick joystick;
    private float horizontalInput;
    private float verticalInput;
    [Header("UI")]
    public TMPro.TextMeshProUGUI fpsText;

    private float fpsUpdateInterval = 0.5f; // Cập nhật FPS nửa giây một lần
    private float fpsAccumulator = 0f;
    private int fpsFrames = 0;
    private float fpsTimeLeft;

    void Start()
    {
        fpsTimeLeft = fpsUpdateInterval;
    }
    public void Fire()
    {
        player.Attack();
    }
    public void SwitchWeapon()
    {
        player.SwitchToNextWeapon();
    }
    public void ThrowBoom()
    {
        player.ThrowBoom();
    }
    public void Dash()
    {
        player.Dash();
    }
    void Update()
    {
        // Tự động đồng bộ: Hễ game dừng (TimeScale = 0) thì toàn bộ âm thanh sẽ tự động dừng
        AudioListener.pause = (Time.timeScale == 0f);

        // 🧮 Tính toán FPS
        fpsTimeLeft -= Time.unscaledDeltaTime;
        fpsAccumulator += Time.unscaledDeltaTime;
        fpsFrames++;

        if (fpsTimeLeft <= 0.0)
        {
            float currentFps = fpsFrames / fpsAccumulator;
            if (fpsText != null)
                fpsText.text = $"FPS: {Mathf.RoundToInt(currentFps)}";
            
            fpsTimeLeft = fpsUpdateInterval;
            fpsAccumulator = 0.0f;
            fpsFrames = 0;
        }

        // 🕹️ Lấy Input di chuyển
        horizontalInput = joystick.Horizontal;
        verticalInput = joystick.Vertical;
        player.InputMove(horizontalInput, verticalInput);

        // 🎯 BẮN: Chuột trái hoặc Space
        if (Input.GetKeyDown(KeyCode.Space))
        {
            player.Attack();
        }

        // 💣 Ném BOOM: G
        if (Input.GetKeyDown(KeyCode.G))
        {
            player.ThrowBoom();
        }

        // 💨 DASH: Left Shift
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            player.Dash();
        }

        // 🔫 CHUYỂN SÚNG TIẾP THEO: E hoặc Scroll lên
        if (Input.GetKeyDown(KeyCode.E) || Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            player.SwitchToNextWeapon();
        }

        // 🔫 CHUYỂN SÚNG TRƯỚC ĐÓ: Q hoặc Scroll xuống
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            player.SwitchToPreviousWeapon();
        }

        // ⚡ NẠP ĐẠN: R
        if (Input.GetKeyDown(KeyCode.R))
        {
            player.ReloadCurrentWeapon();
        }

        // Debug UI
        if (Time.frameCount % 30 == 0)  // Hiển thị mỗi 0.5 giây
        {
            Debug.Log($"[{player.GetCurrentWeaponName()}] Đạn: {player.GetCurrentAmmo()}");
        }
    }
}
