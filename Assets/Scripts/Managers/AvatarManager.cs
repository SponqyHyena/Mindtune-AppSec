using System;
using System.Collections.Generic;
using System.IO;
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

        string avatarPath = GetAvatarPath(userId);

        if (!File.Exists(avatarPath))
        {
            DebugLogger.Log("[AvatarManager] No avatar found for user");
            return;
        }

        try
        {
            FileInfo fileInfo = new FileInfo(avatarPath);
            if (fileInfo.Length > MaxFileSizeBytes)
            {
                ShowError($"Файл слишком большой: {fileInfo.Length / 1024} KB (макс. {MaxFileSizeBytes / 1024} KB)");
                File.Delete(avatarPath);
                return;
            }

            string extension = Path.GetExtension(avatarPath).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                ShowError($"Неподдерживаемый формат: {extension}");
                File.Delete(avatarPath);
                return;
            }

            byte[] bytes = File.ReadAllBytes(avatarPath);

            if (!IsValidImage(bytes))
            {
                ShowError("Файл поврежден или не является изображением");
                File.Delete(avatarPath);
                return;
            }

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (tex.LoadImage(bytes))
            {
                ApplyAvatar(tex);
                ShowSuccess("Фото профиля загружено");
            }
            else
            {
                ShowError("Не удалось загрузить изображение");
                Destroy(tex);
            }
        }
        catch (Exception e)
        {
            ShowError($"Ошибка загрузки: {e.Message}");
            DebugLogger.LogError($"[AvatarManager] Avatar load error: {e.Message}");
        }
    }

    public void LoadImageFromGallery()
    {
#if UNITY_ANDROID
        if (Application.platform == RuntimePlatform.Android)
        {
            var permissions = new string[]
            {
            "android.permission.READ_MEDIA_IMAGES",
            "android.permission.READ_EXTERNAL_STORAGE"
            };

            foreach (var perm in permissions)
            {
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(perm))
                {
                    UnityEngine.Android.Permission.RequestUserPermission(perm);
                }
            }
        }
#endif

#if UNITY_ANDROID || UNITY_IOS

        NativeGallery.GetImageFromGallery((path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                ShowError("Файл не выбран");
                return;
            }

            DebugLogger.Log($"[AvatarManager] Selected path: {path}");

            if (!IsSafePath(path))
            {
                ShowError("Некорректный путь к файлу");
                return;
            }

            if (!File.Exists(path))
            {
                ShowError("Файл не найден");
                return;
            }

            FileInfo fileInfo = new FileInfo(path);
            if (fileInfo.Length > MaxFileSizeBytes)
            {
                ShowError($"Файл слишком большой: {fileInfo.Length / 1024} KB (макс. {MaxFileSizeBytes / 1024} KB)");
                return;
            }
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                ShowError($"Неподдерживаемый формат: {extension}. Используйте PNG или JPG");
                return;
            }

            Texture2D tex = null;
            try
            {
                tex = LoadImageWithFallback(path);

                if (tex == null)
                {
                    ShowError("Не удалось загрузить изображение. Попробуйте другой файл");
                    return;
                }

                if (tex.width == 0 || tex.height == 0)
                {
                    ShowError("Изображение повреждено");
                    Destroy(tex);
                    return;
                }

                if (tex.width > MaxAvatarSize || tex.height > MaxAvatarSize)
                {
                    Texture2D resized = ResizeTexture(tex, MaxAvatarSize, MaxAvatarSize);
                    Destroy(tex);
                    tex = resized;
                }

                SaveAvatarToFile(tex, UserManager.CurrentUser.UserID);
                LoadAvatarForCurrentUser();
                ShowSuccess("Фото профиля обновлено!");

                var userManager = UnityEngine.Object.FindFirstObjectByType<UserManager>();
                if (userManager != null)
                    userManager.SaveUsersData();

                Destroy(tex);
            }
            catch (Exception e)
            {
                ShowError($"Ошибка обработки: {e.Message}");
                DebugLogger.LogError($"[AvatarManager] Error processing image: {e.Message}");
                if (tex != null) Destroy(tex);
            }
        }, "Выберите фото профиля", "image/*");
#else
        ShowError("Доступ к галерее доступен только на мобильных устройствах");
        Debug.LogWarning("Gallery access only on Android/iOS");
#endif
    }

    private static Texture2D LoadImageWithFallback(string path)
    {
        Texture2D tex = null;

        try
        {
            tex = NativeGallery.LoadImageAtPath(path, MaxAvatarSize, false);
            if (tex != null && tex.width > 0 && tex.height > 0)
            {
                DebugLogger.Log("[AvatarManager] Image loaded via NativeGallery");
                return tex;
            }
        }
        catch (Exception e)
        {
            DebugLogger.LogWarning($"[AvatarManager] NativeGallery load failed: {e.Message}");
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            tex = new Texture2D(2, 2);
            if (tex.LoadImage(bytes))
            {
                if (tex.width > MaxAvatarSize || tex.height > MaxAvatarSize)
                {
                    tex = ResizeTexture(tex, MaxAvatarSize, MaxAvatarSize);
                }
                DebugLogger.Log("[AvatarManager] Image loaded via File.ReadAllBytes");
                return tex;
            }
        }
        catch (Exception e)
        {
            DebugLogger.LogWarning($"[AvatarManager] File.ReadAllBytes failed: {e.Message}");
        }

        return null;
    }

    private static void SaveAvatarToFile(Texture2D texture, string userId)
    {
        try
        {
            string safeUserId = SanitizeUserId(userId);
            if (string.IsNullOrEmpty(safeUserId))
            {
                throw new ArgumentException("Invalid user ID");
            }

            string folderPath = Path.Combine(Application.persistentDataPath, AvatarFolder);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = GetAvatarPath(safeUserId);

            string fullPath = Path.GetFullPath(filePath);
            string fullFolder = Path.GetFullPath(folderPath);
            if (!fullPath.StartsWith(fullFolder))
            {
                throw new SecurityException("Path traversal detected");
            }

            byte[] pngData = texture.EncodeToPNG();

            if (pngData.Length > MaxFileSizeBytes)
            {
                throw new Exception($"Avatar too large: {pngData.Length} bytes");
            }

            File.WriteAllBytes(filePath, pngData);

            DebugLogger.Log($"[AvatarManager] Avatar saved: {filePath}");
        }
        catch (Exception e)
        {
            ShowError($"Ошибка сохранения: {e.Message}");
            DebugLogger.LogError($"[AvatarManager] Failed to save avatar: {e.Message}");
            throw;
        }
    }

    private static string GetAvatarPath(string userId)
    {
        string folder = Path.Combine(Application.persistentDataPath, AvatarFolder);
        return Path.Combine(folder, $"{userId}.png");
    }

    private static string SanitizeUserId(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return null;
        string sanitized = Regex.Replace(userId, @"[^a-zA-Z0-9\-]", "");
        if (sanitized.Length > 100)
            sanitized = sanitized.Substring(0, 100);
        return sanitized;
    }

    private static bool IsSafePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        try
        {
            string normalizedPath = Path.GetFullPath(path);

            string extension = Path.GetExtension(path).ToLowerInvariant();
            return AllowedExtensions.Contains(extension);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidImage(byte[] data)
    {
        if (data == null || data.Length < 8) return false;

        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
        {
            return true;
        }

        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
        {
            return true;
        }

        return false;
    }

    private static Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (targetWidth <= 0 || targetHeight <= 0) throw new ArgumentException("Invalid target dimensions");

        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        rt.filterMode = FilterMode.Bilinear;

        RenderTexture.active = rt;
        Graphics.Blit(source, rt);

        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    private static void ApplyAvatar(Texture2D tex)
    {
        if (tex == null)
        {
            ShowError("Не удалось создать изображение");
            return;
        }

        Rect rect = new Rect(0, 0, tex.width, tex.height);
        Vector2 pivot = new Vector2(0.5f, 0.5f);

        Sprite newSprite = Sprite.Create(tex, rect, pivot);

        if (currentAvatar != null)
        {
            if (currentAvatar.texture != null)
                UnityEngine.Object.Destroy(currentAvatar.texture);
            UnityEngine.Object.Destroy(currentAvatar);
        }

        currentAvatar = newSprite;
        OnAvatarChanged?.Invoke(newSprite);

        HideError();
    }

    public static void DeleteAvatar(string userId)
    {
        try
        {
            string safeUserId = SanitizeUserId(userId);
            if (string.IsNullOrEmpty(safeUserId)) return;

            string path = GetAvatarPath(safeUserId);
            if (File.Exists(path))
            {
                File.Delete(path);
                DebugLogger.Log($"[AvatarManager] Avatar deleted: {path}");
            }

            if (currentAvatar != null)
            {
                if (currentAvatar.texture != null)
                    UnityEngine.Object.Destroy(currentAvatar.texture);
                UnityEngine.Object.Destroy(currentAvatar);
                currentAvatar = null;
            }

            ShowSuccess("Аватар удален");
        }
        catch (Exception e)
        {
            ShowError($"Ошибка удаления: {e.Message}");
            DebugLogger.LogError($"[AvatarManager] Failed to delete avatar: {e.Message}");
        }
    }

    private static void ShowError(string message)
    {
        DebugLogger.LogError($"[UI Error] {message}");
        OnAvatarError?.Invoke(message);

        if (instance != null)
        {
            instance.ShowErrorUI(message);
        }
    }

    private void ShowErrorUI(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
        }

        if (errorPanel != null)
        {
            errorPanel.SetActive(true);

            if (errorCoroutine != null)
                StopCoroutine(errorCoroutine);
            errorCoroutine = StartCoroutine(HideErrorAfterDelay(errorDisplayTime));
        }
    }

    private static void ShowSuccess(string message)
    {
        DebugLogger.Log($"[UI Success] {message}");

        if (instance != null)
        {
            instance.ShowSuccessUI(message);
        }
    }

    private void ShowSuccessUI(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
        }

        if (errorPanel != null)
        {
            errorPanel.SetActive(true);

            if (errorCoroutine != null)
                StopCoroutine(errorCoroutine);
            errorCoroutine = StartCoroutine(HideErrorAfterDelay(errorDisplayTime));
        }
    }

    private static void HideError()
    {
        if (instance != null)
        {
            instance.HideErrorUI();
        }
    }

    private void HideErrorUI()
    {
        if (errorPanel != null)
            errorPanel.SetActive(false);

        if (errorText != null)
            errorText.text = "";

        if (errorCoroutine != null)
        {
            StopCoroutine(errorCoroutine);
            errorCoroutine = null;
        }
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
            if (currentAvatar.texture != null)
                UnityEngine.Object.Destroy(currentAvatar.texture);
            UnityEngine.Object.Destroy(currentAvatar);
            currentAvatar = null;
        }
    }
}

public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
}