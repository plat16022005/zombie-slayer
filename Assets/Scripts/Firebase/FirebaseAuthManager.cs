using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class FirebaseAuthManager : MonoBehaviour
{
    public static FirebaseAuthManager Instance { get; private set; }

    [Header("Firebase Objects")]
    private FirebaseAuth auth;
    private FirebaseUser user;

    [Header("Events (Kéo UI Text/Cửa sổ thông báo vào đây)")]
    public UnityEvent<string> OnLoginSuccess;
    public UnityEvent<string> OnLoginFailed;
    public UnityEvent<string> OnRegisterSuccess;
    public UnityEvent<string> OnRegisterFailed;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Khởi tạo Firebase
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Khởi tạo Auth
                auth = FirebaseAuth.DefaultInstance;
                auth.StateChanged += AuthStateChanged;
                AuthStateChanged(this, null);
                Debug.Log("Firebase khởi tạo thành công!");
            }
            else
            {
                Debug.LogError($"Lỗi khởi tạo Firebase: {dependencyStatus}");
            }
        });
    }

    // Lắng nghe sự kiện đăng nhập / đăng xuất
    private void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
            }
            user = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
            }
        }
    }

    // ================== 1. ĐĂNG KÝ (EMAIL & PASSWORD) ==================
    public void RegisterUser(string email, string password)
    {
        Debug.Log($"[Firebase] Đang thử đăng ký tài khoản với email: {email}");

        if (auth == null)
        {
            Debug.LogError("[Firebase] Lỗi: auth object bị NULL. Firebase chưa được khởi tạo!");
            OnRegisterFailed?.Invoke("Hệ thống chưa sẵn sàng!");
            return;
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("[Firebase] Email hoặc Password trống.");
            OnRegisterFailed?.Invoke("Email hoặc Password không được để trống!");
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            try 
            {
                if (task.IsCanceled)
                {
                    Debug.LogWarning("[Firebase] Quá trình đăng ký bị hủy bỏ.");
                    OnRegisterFailed?.Invoke("Đăng ký bị hủy.");
                    return;
                }
                if (task.IsFaulted)
                {
                    FirebaseException firebaseEx = task.Exception?.GetBaseException() as FirebaseException;
                    string errorMsg = firebaseEx != null ? firebaseEx.Message : task.Exception?.Message ?? "Lỗi không xác định";
                    
                    if (firebaseEx != null)
                    {
                        AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                        Debug.LogWarning($"[Firebase] Lỗi Đăng Ký - Mã lỗi: {errorCode} - {errorMsg}");
                    }
                    
                    OnRegisterFailed?.Invoke("Lỗi đăng ký: " + errorMsg);
                    return;
                }

                // Đăng ký thành công
                AuthResult result = task.Result;
                FirebaseUser newUser = result.User;
                Debug.LogFormat("[Firebase] Đăng ký THÀNH CÔNG User: {0} ({1})", newUser.DisplayName, newUser.Email);
                
                OnRegisterSuccess?.Invoke("Đăng ký thành công!");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Firebase] Lỗi Exception trong callback Register: {ex.Message}");
                OnRegisterFailed?.Invoke("Lỗi xử lý hệ thống!");
            }
        });
    }

    // ================== 2. ĐĂNG NHẬP (EMAIL & PASSWORD) ==================
    public void LoginUser(string email, string password)
    {
        Debug.Log($"[Firebase] Đang thử đăng nhập với email: {email}");

        if (auth == null)
        {
            Debug.LogError("[Firebase] Lỗi: auth object bị NULL. Firebase chưa được khởi tạo!");
            OnLoginFailed?.Invoke("Hệ thống chưa sẵn sàng!");
            return;
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("[Firebase] Email hoặc Password trống.");
            OnLoginFailed?.Invoke("Email hoặc Password không được để trống!");
            return;
        }

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            try
            {
                if (task.IsCanceled)
                {
                    Debug.LogWarning("[Firebase] Quá trình đăng nhập bị hủy.");
                    OnLoginFailed?.Invoke("Đăng nhập bị hủy.");
                    return;
                }
                if (task.IsFaulted)
                {
                    FirebaseException firebaseEx = task.Exception?.GetBaseException() as FirebaseException;
                    string errorMsg = firebaseEx != null ? firebaseEx.Message : task.Exception?.Message ?? "Lỗi không xác định";
                    
                    if (firebaseEx != null)
                    {
                        AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                        Debug.LogWarning($"[Firebase] Lỗi Đăng Nhập - Mã lỗi: {errorCode} - {errorMsg}");
                    }

                    OnLoginFailed?.Invoke("Lỗi đăng nhập: " + errorMsg);
                    return;
                }

                // Đăng nhập thành công
                AuthResult result = task.Result;
                FirebaseUser signedInUser = result.User;
                Debug.LogFormat("[Firebase] Đăng nhập THÀNH CÔNG: {0} ({1}) - UID: {2}", signedInUser.DisplayName, signedInUser.Email, signedInUser.UserId);
                
                OnLoginSuccess?.Invoke("Đăng nhập thành công!");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Firebase] Lỗi Exception trong callback Login: {ex.Message}");
                OnLoginFailed?.Invoke("Lỗi xử lý hệ thống!");
            }
        });
    }

    // ================== 3. ĐĂNG NHẬP BẰNG GOOGLE ==================
    /// <summary>
    /// Để dùng được cái này, bạn phải cài thêm package "Google Sign-In Unity Plugin"
    /// Link: https://github.com/googlesamples/google-signin-unity
    /// Plugin đó sẽ pop up cửa sổ Google để lấy idToken, sau đó bạn truyền idToken vào hàm này.
    /// </summary>
    public void SignInWithGoogle(string idToken, string accessToken)
    {
        Debug.Log("[Firebase] Bắt đầu đăng nhập bằng Google Token...");

        if (auth == null)
        {
            OnLoginFailed?.Invoke("Hệ thống chưa sẵn sàng!");
            return;
        }

        Credential credential = GoogleAuthProvider.GetCredential(idToken, accessToken);
        
        auth.SignInAndRetrieveDataWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
        {
            try
            {
                if (task.IsCanceled)
                {
                    Debug.LogWarning("[Firebase] Đăng nhập Google bị hủy.");
                    OnLoginFailed?.Invoke("Đăng nhập Google bị hủy.");
                    return;
                }
                if (task.IsFaulted)
                {
                    FirebaseException firebaseEx = task.Exception?.GetBaseException() as FirebaseException;
                    string errorMsg = firebaseEx != null ? firebaseEx.Message : task.Exception?.Message ?? "Lỗi không xác định";
                    Debug.LogWarning($"[Firebase] Lỗi Đăng Nhập Google: {errorMsg}");
                    
                    OnLoginFailed?.Invoke("Lỗi đăng nhập Google: " + errorMsg);
                    return;
                }

                AuthResult result = task.Result;
                FirebaseUser signedInUser = result.User;
                Debug.LogFormat("[Firebase] Google SignIn THÀNH CÔNG: {0} ({1}) - UID: {2}", signedInUser.DisplayName, signedInUser.Email, signedInUser.UserId);
                
                OnLoginSuccess?.Invoke("Đăng nhập Google thành công!");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Firebase] Lỗi Exception trong callback Google: {ex.Message}");
                OnLoginFailed?.Invoke("Lỗi xử lý hệ thống!");
            }
        });
    }

    // ================== ĐĂNG XUẤT ==================
    public void SignOut()
    {
        if (auth != null)
        {
            auth.SignOut();
            Debug.Log("Đã đăng xuất.");
            SceneManager.LoadScene("StartGame");
        }
    }

    public FirebaseUser GetCurrentUser()
    {
        return auth != null ? auth.CurrentUser : null;
    }

    // Xoá bỏ MainThreadDispatcher ở đây vì Firebase.Extensions đã làm thay.
}
