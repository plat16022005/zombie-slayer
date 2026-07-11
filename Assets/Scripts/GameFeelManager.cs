using UnityEngine;
using System.Collections;
using Cinemachine;

public class GameFeelManager : MonoBehaviour
{
    public static GameFeelManager Instance { get; private set; }

    private CinemachineVirtualCamera vcam;
    private CinemachineBasicMultiChannelPerlin perlin;

    private float shakeTimer = 0f;
    private float shakeTimerTotal = 0f;
    private float startingMagnitude = 0f;

    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
            return;
        }
        
        // Tự động tìm Cinemachine Virtual Camera trong Scene
        vcam = FindObjectOfType<CinemachineVirtualCamera>();
        if (vcam != null)
        {
            perlin = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (perlin == null)
            {
                Debug.LogWarning("Cinemachine chưa được thêm Noise (Basic Multi Channel Perlin). GameFeelManager không thể rung màn hình.");
            }
        }
    }

    /// <summary>
    /// Rung màn hình bằng Cinemachine
    /// </summary>
    public void ShakeCamera(float duration, float magnitude)
    {
        if (perlin != null)
        {
            perlin.m_AmplitudeGain = magnitude;
            startingMagnitude = magnitude;
            shakeTimer = duration;
            shakeTimerTotal = duration;
        }
    }

    private void Update()
    {
        // Giảm dần độ rung về 0
        if (perlin != null)
        {
            if (shakeTimer > 0)
            {
                shakeTimer -= Time.unscaledDeltaTime;
                perlin.m_AmplitudeGain = Mathf.Lerp(startingMagnitude, 0f, 1f - (shakeTimer / shakeTimerTotal));
            }
            else
            {
                perlin.m_AmplitudeGain = 0f;
            }
        }
    }

    /// <summary>
    /// Dừng đọng thời gian (Hit Stop) để tạo cảm giác uy lực khi trúng mục tiêu
    /// </summary>
    /// <param name="duration">Thời gian dừng (giây, khuyên dùng 0.05f - 0.1f)</param>
    public void HitStop(float duration)
    {
        StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0f; // Đóng băng thời gian
        yield return new WaitForSecondsRealtime(duration); // Chờ thời gian thực
        Time.timeScale = 1f; // Trả lại bình thường
    }
}
