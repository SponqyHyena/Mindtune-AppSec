using UnityEngine;

public static class StorageKeys
{
    private static string appPrefix;
    private static bool isInitialized = false;

    public static void Initialize()
    {
        if (isInitialized) return;

        try
        {
            appPrefix = Application.identifier ?? "com.mentalhealth.app";
            isInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize StorageKeys: {e.Message}");
            appPrefix = "com.mentalhealth.app";
            isInitialized = true;
        }
    }

    private static string AppPrefix
    {
        get
        {
            if (!isInitialized)
            {
                Debug.LogWarning("StorageKeys not initialized! Using fallback.");
                return "com.mentalhealth.app";
            }
            return appPrefix;
        }
    }

    public static string UsersList => $"{AppPrefix}.users.list";
    public static string Theme => $"{AppPrefix}.settings.theme";
    public static string AuthToken => $"{AppPrefix}.auth.token";
    public static string LastUser => $"{AppPrefix}.auth.lastuser";

    public static string UserDataPrefix => $"{AppPrefix}.user";
    public static string GetUserDataKey(string userId) => $"{UserDataPrefix}.{userId}.data";

    public static string GetVersionedKey(string baseKey, int version = 1)
    {
        return $"{baseKey}.v{version}";
    }

    public static bool IsValidKey(string key)
    {
        return !string.IsNullOrEmpty(key) && key.StartsWith(AppPrefix);
    }
}