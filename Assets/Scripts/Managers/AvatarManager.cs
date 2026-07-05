using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class AvatarManager : MonoBehaviour
{
    public static event Action<Sprite> OnAvatarChanged;
    public static event Action<string> OnAvatarError;

    private static Sprite currentAvatar;
    public static Sprite CurrentAvatar => currentAvatar;

    [Header("UI References")]
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private float errorDisplayTime = 5f;

    private const string AvatarFolder = "avatars";
    private const int MaxAvatarSize = 1024;
    private const int MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new HashSet<string>
    {
        ".png", ".jpg", ".jpeg"
    };

    private static AvatarManager instance;
    private Coroutine errorCoroutine;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void WebGLOpenFilePicker(
        string gameObjectName, string callbackMethod, int maxSizeBytes);

    [DllImport("__Internal")]
    private static extern string WebGLFetchAvatarData();

    [DllImport("__Internal")]
    private static extern string WebGLFetchAvatarError();
#endif

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        if (errorPanel != null)
            errorPanel.SetActive(false);
    }


    public static void LoadAvatarForCurrentUser()
    {
        if (UserManager.CurrentUser == null)
        {
            ShowError("Пользователь не авторизован");
            return;
        }

        string userId = SanitizeUserId(UserManager.CurrentUser.UserID);
        if (string.IsNullOrEmpty(userId))
        {
            ShowError("Ошибка: неверный ID пользователя");
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        LoadAvatarWebGL(userId);
#else
        LoadAvatarFromFile(userId);
#endif
    }


    public void LoadImageFromGallery()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log($"[AvatarManager] LoadImageFromGallery WebGL, gameObject.name='{gameObject.name}'");
        WebGLOpenFilePicker(gameObject.name, "OnWebGLImageLoaded", MaxFileSizeBytes);
#elif UNITY_ANDROID
        RequestAndroidPermissionsAndOpenGallery();
#elif UNITY_IOS
        OpenNativeGallery();
#else
        ShowError("Выбор фото недоступен на этой платформе");
#endif
    }

    public void OnWebGLImageLoaded(string _)
    {
#if UNITY_WEBGL && !UNITY_EDITOR

        string base64 = WebGLFetchAvatarData();

        if (string.IsNullOrEmpty(base64))
        {
            ShowError("Ошибка получения данных изображения");
            return;
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64);
        }
        catch (FormatException ex)
        {
            ShowError("Ошибка декодирования изображения");
            return;
        }

        if (bytes.Length > MaxFileSizeBytes)
        {
            ShowError($"Файл слишком большой: {bytes.Length / 1024} KB (макс. {MaxFileSizeBytes / 1024} KB)");
            return;
        }

        if (!IsValidImage(bytes))
        {
            ShowError("Файл повреждён или не является изображением PNG/JPG");
            return;
        }


        Texture2D tex = null;
        try
        {
            tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            bool loaded = tex.LoadImage(bytes);

            if (!loaded)
            {
                ShowError("Не удалось декодировать изображение");
                Destroy(tex);
                return;
            }

            if (tex.width == 0 || tex.height == 0)
            {
                ShowError("Изображение повреждено (нулевые размеры)");
                Destroy(tex);
                return;
            }


            SaveAvatarWebGL(bytes, UserManager.CurrentUser.UserID);
            ApplyAvatar(tex);
            ShowSuccess("Фото профиля обновлено!");

            var userManager = FindFirstObjectByType<UserManager>();
            if (userManager != null)
                userManager.SaveUsersData();
        }
        catch (Exception e)
        {
            ShowError($"Ошибка обработки изображения: {e.Message}");
            if (tex != null) Destroy(tex);
        }
#endif
    }

    public void OnWebGLPickerError(string _)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string errorCode = WebGLFetchAvatarError() ?? "UNKNOWN";

        if (errorCode.StartsWith("TOO_LARGE:"))
        {
            long.TryParse(errorCode.Substring("TOO_LARGE:".Length), out long size);
            ShowError($"Файл слишком большой: {size / 1024} KB (макс. {MaxFileSizeBytes / 1024} KB)");
        }
        else if (errorCode.StartsWith("INVALID_TYPE:"))
        {
            string type = errorCode.Substring("INVALID_TYPE:".Length);
            ShowError($"Неподдерживаемый формат: {type}. Используйте PNG или JPG");
        }
        else if (errorCode.StartsWith("READ_ERROR:"))
        {
            ShowError("Ошибка чтения файла. Попробуйте другой файл");
        }
        else
        {
            ShowError("Ошибка при загрузке файла");
        }
#endif
    }

    public void OnWebGLPickerCancelled(string _)
    {
        Debug.Log("WebGL file picker cancelled");
    }


#if UNITY_ANDROID
    private void RequestAndroidPermissionsAndOpenGallery()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            var permissions = new[]
            {
                "android.permission.READ_MEDIA_IMAGES",
                "android.permission.READ_EXTERNAL_STORAGE"
            };
            foreach (var perm in permissions)
            {
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(perm))
                    UnityEngine.Android.Permission.RequestUserPermission(perm);
            }
        }
        OpenNativeGallery();
    }
#endif

#if UNITY_ANDROID || UNITY_IOS
    private void OpenNativeGallery()
    {
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (string.IsNullOrEmpty(path)) { ShowError("Файл не выбран"); return; }
            if (!IsSafePath(path)) { ShowError("Некорректный путь к файлу"); return; }
            if (!File.Exists(path)) { ShowError("Файл не найден"); return; }

            FileInfo fi = new FileInfo(path);
            if (fi.Length > MaxFileSizeBytes)
            {
                ShowError($"Файл слишком большой: {fi.Length / 1024} KB (макс. {MaxFileSizeBytes / 1024} KB)");
                return;
            }

            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                ShowError($"Неподдерживаемый формат: {ext}. Используйте PNG или JPG");
                return;
            }

            Texture2D tex = null;
            try
            {
                tex = LoadImageWithFallback(path);
                if (tex == null) { ShowError("Не удалось загрузить изображение"); return; }
                if (tex.width == 0 || tex.height == 0) { ShowError("Изображение повреждено"); Destroy(tex); return; }

                if (tex.width > MaxAvatarSize || tex.height > MaxAvatarSize)
                {
                    Texture2D r = ResizeTexture(tex, MaxAvatarSize, MaxAvatarSize);
                    Destroy(tex);
                    tex = r;
                }

                SaveAvatarToFile(tex, UserManager.CurrentUser.UserID);
                LoadAvatarForCurrentUser();
                ShowSuccess("Фото профиля обновлено!");

                var um = FindFirstObjectByType<UserManager>();
                if (um != null) um.SaveUsersData();
                Destroy(tex);
            }
            catch (Exception e)
            {
                ShowError($"Ошибка обработки: {e.Message}");
                if (tex != null) Destroy(tex);
            }
        }, "Выберите фото профиля", "image/*");
    }

    private static Texture2D LoadImageWithFallback(string path)
    {
        try
        {
            Texture2D t = NativeGallery.LoadImageAtPath(path, MaxAvatarSize, false);
            if (t != null && t.width > 0) return t;
        }
        catch (Exception e) { DebugLogger.LogWarning($"NativeGallery failed: {e.Message}"); }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            if (!IsValidImage(bytes)) return null;
            Texture2D t = new Texture2D(2, 2);
            if (t.LoadImage(bytes))
            {
                if (t.width > MaxAvatarSize || t.height > MaxAvatarSize)
                    t = ResizeTexture(t, MaxAvatarSize, MaxAvatarSize);
                return t;
            }
        }
        catch (Exception e) { DebugLogger.LogWarning($"ReadAllBytes failed: {e.Message}"); }

        return null;
    }
#endif


    private static void LoadAvatarFromFile(string userId)
    {
        string path = GetAvatarPath(userId);
        if (!File.Exists(path)) { DebugLogger.Log("[AvatarManager] No avatar file"); return; }

        try
        {
            FileInfo fi = new FileInfo(path);
            if (fi.Length > MaxFileSizeBytes) { File.Delete(path); ShowError("Файл слишком большой"); return; }

            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext)) { File.Delete(path); ShowError($"Неподдерживаемый формат: {ext}"); return; }

            byte[] bytes = File.ReadAllBytes(path);
            if (!IsValidImage(bytes)) { File.Delete(path); ShowError("Файл повреждён"); return; }

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(bytes)) ApplyAvatar(tex);
            else { ShowError("Не удалось загрузить изображение"); Destroy(tex); }
        }
        catch (Exception e)
        {
            ShowError($"Ошибка загрузки: {e.Message}");
        }
    }

    private static void SaveAvatarToFile(Texture2D texture, string userId)
    {
        string safeId = SanitizeUserId(userId);
        if (string.IsNullOrEmpty(safeId)) throw new ArgumentException("Invalid user ID");

        string folder = Path.Combine(Application.persistentDataPath, AvatarFolder);
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        string filePath = GetAvatarPath(safeId);
        if (!Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(folder)))
            throw new SecurityException("Path traversal detected");

        byte[] png = texture.EncodeToPNG();
        if (png.Length > MaxFileSizeBytes) throw new Exception($"Avatar too large: {png.Length}");
        File.WriteAllBytes(filePath, png);
    }


    private static void LoadAvatarWebGL(string userId)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string folder = Path.Combine(Application.persistentDataPath, AvatarFolder);
            string filePath = Path.Combine(folder, $"{userId}.img");

            if (!File.Exists(filePath))
            {
                return;
            }

            byte[] bytes = File.ReadAllBytes(filePath);

            if (bytes.Length > MaxFileSizeBytes)
            {
                File.Delete(filePath);
                ShowError("Сохранённый аватар слишком большой");
                return;
            }

            if (!IsValidImage(bytes))
            {
                File.Delete(filePath);
                ShowError("Сохранённый аватар повреждён");
                return;
            }

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(bytes))
                ApplyAvatar(tex);
            else
            {
                ShowError("Не удалось загрузить сохранённый аватар");
                Destroy(tex);
            }
        }
        catch (Exception e)
        {
            ShowError($"Ошибка загрузки аватара: {e.Message}");
        }
#endif
    }


    private static void SaveAvatarWebGL(byte[] imageBytes, string userId)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string safeId = SanitizeUserId(userId);
        if (string.IsNullOrEmpty(safeId)) throw new ArgumentException("Invalid user ID");
        if (imageBytes == null || imageBytes.Length == 0) throw new ArgumentException("Empty image bytes");

        string folder = Path.Combine(Application.persistentDataPath, AvatarFolder);
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        string filePath = Path.Combine(folder, $"{safeId}.img");

        if (!Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(folder)))
            throw new SecurityException("Path traversal detected");

        File.WriteAllBytes(filePath, imageBytes);
#endif
    }


    public static void DeleteAvatar(string userId)
    {
        try
        {
            string safeId = SanitizeUserId(userId);
            if (string.IsNullOrEmpty(safeId)) return;

#if UNITY_WEBGL && !UNITY_EDITOR
            string webGLFolder = Path.Combine(Application.persistentDataPath, AvatarFolder);
            string webGLPath = Path.Combine(webGLFolder, $"{safeId}.img");
            if (File.Exists(webGLPath)) File.Delete(webGLPath);
#else
            string path = GetAvatarPath(safeId);
            if (File.Exists(path)) File.Delete(path);
#endif
            if (currentAvatar != null)
            {
                if (currentAvatar.texture != null) UnityEngine.Object.Destroy(currentAvatar.texture);
                UnityEngine.Object.Destroy(currentAvatar);
                currentAvatar = null;
            }
            ShowSuccess("Аватар удалён");
        }
        catch (Exception e)
        {
            ShowError($"Ошибка удаления: {e.Message}");
        }
    }


    private static string GetAvatarPath(string userId) =>
        Path.Combine(Application.persistentDataPath, AvatarFolder, $"{userId}.png");

    private static string SanitizeUserId(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return null;
        string s = Regex.Replace(userId, @"[^a-zA-Z0-9\-]", "");
        return s.Length > 100 ? s.Substring(0, 100) : s;
    }

    private static bool IsSafePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        try
        {
            Path.GetFullPath(path);
            return AllowedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());
        }
        catch { return false; }
    }

    private static bool IsValidImage(byte[] data)
    {
        if (data == null || data.Length < 8) return false;

        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            return true;

        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return true;
        return false;
    }

    private static Texture2D ResizeTexture(Texture2D source, int targetW, int targetH)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (targetW <= 0 || targetH <= 0) throw new ArgumentException("Invalid target dimensions");

        Texture2D result = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
        Color[] dst = new Color[targetW * targetH];

        float invW = 1f / (targetW > 1 ? targetW - 1 : 1);
        float invH = 1f / (targetH > 1 ? targetH - 1 : 1);

        for (int y = 0; y < targetH; y++)
        {
            float v = y * invH;
            for (int x = 0; x < targetW; x++)
            {
                dst[y * targetW + x] = source.GetPixelBilinear(x * invW, v);
            }
        }

        result.SetPixels(dst);
        result.Apply();
        return result;
    }

    private static void ApplyAvatar(Texture2D tex)
    {
        if (tex == null) { ShowError("Не удалось создать изображение"); return; }
        Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        if (currentAvatar != null)
        {
            if (currentAvatar.texture != null) UnityEngine.Object.Destroy(currentAvatar.texture);
            UnityEngine.Object.Destroy(currentAvatar);
        }
        currentAvatar = s;
        OnAvatarChanged?.Invoke(s);
        HideError();
    }


    private static void ShowError(string msg)
    {
        OnAvatarError?.Invoke(msg);
        instance?.ShowErrorUI(msg);
    }

    private void ShowErrorUI(string msg)
    {
        if (errorText != null) errorText.text = msg;
        if (errorPanel != null)
        {
            errorPanel.SetActive(true);
            if (errorCoroutine != null) StopCoroutine(errorCoroutine);
            errorCoroutine = StartCoroutine(HideErrorAfterDelay(errorDisplayTime));
        }
    }

    private static void ShowSuccess(string msg)
    {
        instance?.ShowSuccessUI(msg);
    }

    private void ShowSuccessUI(string msg)
    {
        if (errorText != null) errorText.text = msg;
        if (errorPanel != null)
        {
            errorPanel.SetActive(true);
            if (errorCoroutine != null) StopCoroutine(errorCoroutine);
            errorCoroutine = StartCoroutine(HideErrorAfterDelay(errorDisplayTime));
        }
    }

    private static void HideError() => instance?.HideErrorUI();

    private void HideErrorUI()
    {
        if (errorPanel != null) errorPanel.SetActive(false);
        if (errorText != null) errorText.text = "";
        if (errorCoroutine != null) { StopCoroutine(errorCoroutine); errorCoroutine = null; }
    }

    private System.Collections.IEnumerator HideErrorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideErrorUI();
    }

    private void OnDestroy()
    {
        if (currentAvatar != null)
        {
            if (currentAvatar.texture != null) UnityEngine.Object.Destroy(currentAvatar.texture);
            UnityEngine.Object.Destroy(currentAvatar);
            currentAvatar = null;
        }
    }
}

public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
}