using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using UnityEngine.UI; // Thêm thư viện UI cho Slider

public class LoadingManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Slider progressBar; // Kéo UI Slider vào đây
    [SerializeField] private TextMeshProUGUI progressText; // Kéo Text % vào đây

    private bool isDataLoaded = false;

    private void Start()
    {
        UpdateProgressUI(0f);
        if (loadingText != null) loadingText.text = "Đang kết lấy dữ liệu người dùng...";

        // Bắt đầu hiệu ứng loading
        StartCoroutine(LoadingRoutine());

        // Bắt đầu tải dữ liệu thông qua DataGame
        if (DataGame.Instance != null)
        {
            DataGame.Instance.LoadUserData(() => {
                isDataLoaded = true; // Đánh dấu đã tải xong dữ liệu từ mạng
            });
        }
        else
        {
            Debug.LogError("Không tìm thấy DataGame Singleton trong scene! Hãy chắc chắn đã tạo DataGame.");
            isDataLoaded = true; // Vẫn cho qua scene MainMenu để xử lý lỗi hoặc test
        }
    }

    private void UpdateProgressUI(float progress)
    {
        if (progressBar != null) progressBar.value = progress;
        if (progressText != null) progressText.text = Mathf.RoundToInt(progress * 100f) + "%";
    }

    private IEnumerator LoadingRoutine()
    {
        float targetProgress = 0f;
        float currentProgress = 0f;

        // --- GIAI ĐOẠN 1: TẢI DỮ LIỆU TỪ FIREBASE ---
        // Trong lúc chờ tải mạng, cho thanh chạy ảo từ 0% đến 40%
        while (!isDataLoaded)
        {
            // Tăng nhẹ targetProgress mỗi frame (tối đa lên 0.4)
            targetProgress = Mathf.MoveTowards(targetProgress, 0.4f, Time.deltaTime * 0.3f); 
            // Lerp để thanh chạy mượt
            currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * 5f);
            
            UpdateProgressUI(currentProgress);
            
            yield return null;
        }

        // Dữ liệu đã tải xong. Chờ thanh loading chạy vọt lên mốc 40% (nếu mạng load quá nhanh)
        while (currentProgress < 0.4f)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, 0.4f, Time.deltaTime * 2f);
            UpdateProgressUI(currentProgress);
            yield return null;
        }


        // --- GIAI ĐOẠN 2: TẢI SCENE BẤT ĐỒNG BỘ ---
        if (loadingText != null) loadingText.text = "Đang chuẩn bị vào game...";
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainMenu");
        asyncLoad.allowSceneActivation = false; // Ngăn tự động chuyển scene ngay khi tải xong

        while (!asyncLoad.isDone)
        {
            // Tiến trình của Unity scene load chạy từ 0.0 đến 0.9
            float sceneProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            // Map từ 0-1 (sceneProgress) thành 0.4-1.0 cho thanh tổng
            float finalTarget = Mathf.Lerp(0.4f, 1f, sceneProgress);

            currentProgress = Mathf.MoveTowards(currentProgress, finalTarget, Time.deltaTime * 2f);
            UpdateProgressUI(currentProgress);

            // Nếu thanh loading đã đầy 100%
            if (currentProgress >= 1f)
            {
                if (loadingText != null) loadingText.text = "Tải hoàn tất!";
                // Đợi 0.5s cho mượt mắt trước khi chuyển scene
                yield return new WaitForSeconds(0.5f); 
                asyncLoad.allowSceneActivation = true; // Chuyển scene!
            }

            yield return null;
        }
    }
}
