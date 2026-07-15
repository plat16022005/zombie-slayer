using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Google;
using System.Threading.Tasks;
using System.Collections.Concurrent;

public class AuthUIManager : MonoBehaviour
{
    private readonly ConcurrentQueue<System.Action> _mainThreadActions = new ConcurrentQueue<System.Action>();

    [SerializeField] private GameObject NotificationPanel;
    [Header("--- LOGIN UI ---")]
    [SerializeField] private TMP_InputField loginEmailInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button googleSignInButton;
    [SerializeField] private TextMeshProUGUI loginStatusText;

    [Header("--- REGISTER UI ---")]
    [SerializeField] private TMP_InputField registerEmailInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private TMP_InputField registerConfirmPasswordInput; // Xác nhận lại mật khẩu (Tùy chọn)
    [SerializeField] private Button registerButton;
    [SerializeField] private TextMeshProUGUI registerStatusText;

    [Header("--- GENERAL ---")]
    [SerializeField] private Button logoutButton; // Thường nằm ở Menu chính sau khi login

    private void Start()
    {
        // Gắn sự kiện cho các nút hành động
        if (loginButton != null) loginButton.onClick.AddListener(OnLoginClicked);
        if (registerButton != null) registerButton.onClick.AddListener(OnRegisterClicked);
        if (googleSignInButton != null) googleSignInButton.onClick.AddListener(OnGoogleSignInClicked);
        if (logoutButton != null) logoutButton.onClick.AddListener(OnLogoutClicked);

        // Đăng ký lắng nghe sự kiện từ FirebaseAuthManager
        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.OnLoginSuccess.AddListener(OnLoginSuccess);
            FirebaseAuthManager.Instance.OnLoginFailed.AddListener(OnLoginFailed);
            
            FirebaseAuthManager.Instance.OnRegisterSuccess.AddListener(OnRegisterSuccess);
            FirebaseAuthManager.Instance.OnRegisterFailed.AddListener(OnRegisterFailed);
        }
    }

    private void OnDestroy()
    {
        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.OnLoginSuccess.RemoveListener(OnLoginSuccess);
            FirebaseAuthManager.Instance.OnLoginFailed.RemoveListener(OnLoginFailed);
            FirebaseAuthManager.Instance.OnRegisterSuccess.RemoveListener(OnRegisterSuccess);
            FirebaseAuthManager.Instance.OnRegisterFailed.RemoveListener(OnRegisterFailed);
        }
    }

    private void Update()
    {
        while (_mainThreadActions.TryDequeue(out var action))
        {
            action?.Invoke();
        }
    }
    
    // ================= HÀNH ĐỘNG =================
    private void OnLoginClicked()
    {
        if (FirebaseAuthManager.Instance == null) return;
        
        ClearStatus();
        string email = loginEmailInput.text.Trim();
        string password = loginPasswordInput.text;
        
        SetStatus(loginStatusText, "Đang xử lý đăng nhập...", Color.yellow);
        FirebaseAuthManager.Instance.LoginUser(email, password);
    }

    private void OnRegisterClicked()
    {
        if (FirebaseAuthManager.Instance == null) return;

        ClearStatus();
        string email = registerEmailInput.text.Trim();
        string password = registerPasswordInput.text;
        
        // Kiểm tra xác nhận mật khẩu (nếu bạn có dùng ô này)
        if (registerConfirmPasswordInput != null && registerConfirmPasswordInput.gameObject.activeInHierarchy)
        {
            string confirmPass = registerConfirmPasswordInput.text;
            if (password != confirmPass)
            {
                SetStatus(registerStatusText, "Mật khẩu xác nhận không khớp!", Color.red);
                return;
            }
        }

        SetStatus(registerStatusText, "Đang xử lý đăng ký...", Color.yellow);
        FirebaseAuthManager.Instance.RegisterUser(email, password);
    }

    private async void OnGoogleSignInClicked()
    {
        if (FirebaseAuthManager.Instance == null) return;

        ClearStatus();
        SetStatus(loginStatusText, "Đang kết nối với Google...", Color.yellow);
        
        GoogleSignIn.Configuration = new GoogleSignInConfiguration {
            RequestIdToken = true,
            RequestEmail = true,
            WebClientId = "959255341112-a9i3bnsar4pirtv7daqlnofbpn6pbupt.apps.googleusercontent.com"
        };

        try
        {
            GoogleSignInUser googleUser = await GoogleSignIn.DefaultInstance.SignIn();
            
            // Lấy được token từ Google thành công, giờ ném sang cho Firebase!
            SetStatus(loginStatusText, "Đang xác thực với hệ thống...", Color.yellow);
            FirebaseAuthManager.Instance.SignInWithGoogle(googleUser.IdToken, null);
        }
        catch (System.Exception ex)
        {
            SetStatus(loginStatusText, "Google Sign-In bị hủy hoặc lỗi: " + ex.Message, Color.red);
        }
    }

    private void OnLogoutClicked()
    {
        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.SignOut();
            SetStatus(loginStatusText, "Đã đăng xuất thành công.", Color.green);
        }
    }

    // ================= XỬ LÝ SỰ KIỆN TỪ FIREBASE =================
    private void OnLoginSuccess(string msg)
    {
        _mainThreadActions.Enqueue(() => {
            SetStatus(loginStatusText, msg, Color.green);
            // Tự động chuyển qua Scene game chính (SampleScene) khi đăng nhập thành công
            SceneManager.LoadScene("Loading");
        });
    }

    private void OnLoginFailed(string msg)
    {
        _mainThreadActions.Enqueue(() => {
            SetStatus(loginStatusText, msg, Color.red);
        });
    }

    private void OnRegisterSuccess(string msg)
    {
        _mainThreadActions.Enqueue(() => {
            SetStatus(registerStatusText, msg, Color.green);
            NotificationPanel.SetActive(true);
            SceneManager.LoadScene("Loading");
        });
    }

    private void OnRegisterFailed(string msg)
    {
        _mainThreadActions.Enqueue(() => {
            SetStatus(registerStatusText, msg, Color.red);
        });
    }

    // ================= TIỆN ÍCH =================
    private void SetStatus(TextMeshProUGUI tmp, string msg, Color color)
    {
        NotificationPanel.SetActive(true);
        if (tmp != null)
        {
            tmp.text = msg;
            tmp.color = color;
        }
    }

    private void ClearStatus()
    {
        if (loginStatusText != null) loginStatusText.text = "";
        if (registerStatusText != null) registerStatusText.text = "";
    }
}
