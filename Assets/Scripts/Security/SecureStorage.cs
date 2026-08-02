using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class SecureStorage
{
    private const int saltSize = 32;
    private const int keySize = 32;
    private const int iterations = 100000;


    public static void SaveEncryptedData<T>(T data, string userId, string password)
    {
        if (data == null || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            Debug.LogError("Data, userId or password is null/empty");
            return;
        }

        try
        {
            string json = SecureJsonSerializer.Serialize(data);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("Serialized JSON is empty");
                return;
            }

            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            byte[] encrypted = Encrypt(jsonBytes, password);

            string filePath = GetFilePath(userId);
            string directory = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllBytes(filePath, encrypted);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SecureStorage] Save error for user {userId}: {e.Message}");
        }
    }

    public static T LoadEncryptedData<T>(string userId, string password, out bool wasLegacyFormat) where T : new()
    {
        wasLegacyFormat = false;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            Debug.LogError("UserId or password is null or empty");
            return new T();
        }

        string filePath = GetFilePath(userId);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"File not found for user {userId}");
            return new T();
        }

        byte[] encrypted;
        try
        {
            encrypted = File.ReadAllBytes(filePath);
            if (encrypted == null || encrypted.Length == 0)
            {
                Debug.LogWarning("Encrypted data is empty");
                return new T();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SecureStorage] Read error for user {userId}: {e.Message}");
            return new T();
        }

        try
        {
            byte[] decrypted = Decrypt(encrypted, password);
            string json = Encoding.UTF8.GetString(decrypted);
            if (string.IsNullOrEmpty(json)) throw new CryptographicException("Empty payload");

            T result = SecureJsonSerializer.Deserialize<T>(json);
            if (result == null) throw new CryptographicException("Deserialization failed");
            return result;
        }
        catch
        {
            try
            {
                byte[] decryptedLegacy = DecryptLegacy(encrypted, userId);
                string json = Encoding.UTF8.GetString(decryptedLegacy);

                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning("Legacy decrypted JSON is empty");
                    return new T();
                }

                T result = SecureJsonSerializer.Deserialize<T>(json);
                if (result == null) return new T();

                wasLegacyFormat = true;
                return result;
            }
            catch (Exception e2)
            {
                Debug.LogError($"[SecureStorage] Load error for user {userId}: {e2.Message}");
                return new T();
            }
        }
    }

    public static T LoadEncryptedData<T>(string userId, string password) where T : new()
    {
        return LoadEncryptedData<T>(userId, password, out _);
    }

    private static byte[] Encrypt(byte[] data, string password)
    {
        using (Aes aes = Aes.Create())
        {
            byte[] salt = new byte[saltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            aes.Key = DeriveKey(password, salt);
            aes.GenerateIV();

            using (var ms = new MemoryStream())
            {
                ms.Write(salt, 0, salt.Length);
                ms.Write(aes.IV, 0, aes.IV.Length);

                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                }

                return ms.ToArray();
            }
        }
    }

    private static byte[] Decrypt(byte[] encrypted, string password)
    {
        using (Aes aes = Aes.Create())
        {
            byte[] salt = new byte[saltSize];
            Array.Copy(encrypted, 0, salt, 0, saltSize);

            aes.Key = DeriveKey(password, salt);

            byte[] iv = new byte[16];
            Array.Copy(encrypted, saltSize, iv, 0, 16);
            aes.IV = iv;

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(encrypted, saltSize + 16, encrypted.Length - saltSize - 16);
                    cs.FlushFinalBlock();
                }
                return ms.ToArray();
            }
        }
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
        {
            return pbkdf2.GetBytes(keySize);
        }
    }

    private static string GetLegacyEncryptionPassword(string userId)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] combined = Encoding.UTF8.GetBytes(userId + Application.identifier);
            byte[] hash = sha256.ComputeHash(combined);
            return Convert.ToBase64String(hash).Substring(0, 32);
        }
    }

    private static void SaveEncryptedDataLegacy<T>(T data, string userId)
    {
        if (data == null || string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Data or userId is null");
            return;
        }

        try
        {
            string json = SecureJsonSerializer.Serialize(data);
            if (string.IsNullOrEmpty(json)) return;

            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            byte[] encrypted = EncryptLegacy(jsonBytes, userId);

            string filePath = GetFilePath(userId);
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            File.WriteAllBytes(filePath, encrypted);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SecureStorage] Legacy save error for user {userId}: {e.Message}");
        }
    }

    private static byte[] EncryptLegacy(byte[] data, string userId)
    {
        using (Aes aes = Aes.Create())
        {
            byte[] salt = new byte[saltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            aes.Key = DeriveLegacyKey(userId, salt);
            aes.GenerateIV();

            using (var ms = new MemoryStream())
            {
                ms.Write(salt, 0, salt.Length);
                ms.Write(aes.IV, 0, aes.IV.Length);

                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                }
                return ms.ToArray();
            }
        }
    }

    private static byte[] DecryptLegacy(byte[] encrypted, string userId)
    {
        using (Aes aes = Aes.Create())
        {
            byte[] salt = new byte[saltSize];
            Array.Copy(encrypted, 0, salt, 0, saltSize);

            aes.Key = DeriveLegacyKey(userId, salt);

            byte[] iv = new byte[16];
            Array.Copy(encrypted, saltSize, iv, 0, 16);
            aes.IV = iv;

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(encrypted, saltSize + 16, encrypted.Length - saltSize - 16);
                    cs.FlushFinalBlock();
                }
                return ms.ToArray();
            }
        }
    }

    private static byte[] DeriveLegacyKey(string userId, byte[] salt)
    {
        string password = GetLegacyEncryptionPassword(userId);
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
        {
            return pbkdf2.GetBytes(keySize);
        }
    }

    private static string GetFilePath(string userId)
    {
        string folder = Path.Combine(Application.persistentDataPath, "userdata");
        return Path.Combine(folder, $"{userId}.enc");
    }

    public static void DeleteUserData(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return;

        try
        {
            string path = GetFilePath(userId);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"User data deleted for {userId}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Delete error for user {userId}: {e.Message}");
        }
    }

    public static bool MigrateOldData()
    {
        const string oldKey = "UserDatabase";

        if (!PlayerPrefs.HasKey(oldKey))
            return false;

        try
        {
            string oldJson = PlayerPrefs.GetString(oldKey);

            if (!IsValidJson(oldJson))
            {
                Debug.LogError("Invalid JSON format in old data");
                return false;
            }

            var oldWrapper = DeserializeOldData(oldJson);
            if (oldWrapper == null || oldWrapper.Users == null || oldWrapper.Users.Count == 0)
            {
                Debug.LogWarning("No users found in old data");
                return false;
            }
            var migrationResult = MigrateUsers(oldWrapper.Users);

            SaveNewUserList(migrationResult.MetaList);
            CleanupOldData(oldKey);

            Debug.Log($"Migration completed: {migrationResult.MigratedCount} users migrated");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Migration failed: {e.Message}");
            return false;
        }
    }

    private static bool IsValidJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return false;

        try
        {
            Newtonsoft.Json.Linq.JToken.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static OldUserWrapper DeserializeOldData(string json)
    {
        try
        {
            return SecureJsonSerializer.Deserialize<OldUserWrapper>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to deserialize old data: {e.Message}");
            return null;
        }
    }

    private static MigrationResult MigrateUsers(List<UserData> oldUsers)
    {
        var result = new MigrationResult
        {
            MetaList = new UserMetaList(),
            MigratedCount = 0
        };

        foreach (var user in oldUsers)
        {
            if (user == null || string.IsNullOrEmpty(user.UserID))
                continue;

            try
            {
                SaveEncryptedDataLegacy(user, user.UserID);

                result.MetaList.Users.Add(new UserMeta
                {
                    Username = user.Username ?? "",
                    UserID = user.UserID ?? "",
                    Name = user.Name ?? "",
                    Email = user.Email ?? "",
                    Role = user.Role ?? "User"
                });

                result.MigratedCount++;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to migrate user {user.UserID}: {e.Message}");
            }
        }

        return result;
    }

    private static void SaveNewUserList(UserMetaList metaList)
    {
        try
        {
            string newJson = SecureJsonSerializer.Serialize(metaList);
            if (!string.IsNullOrEmpty(newJson))
            {
                PlayerPrefs.SetString(StorageKeys.UsersList, newJson);
                PlayerPrefs.Save();
                Debug.Log($"New user list saved with {metaList.Users.Count} users");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save new user list: {e.Message}");
        }
    }

    private static void CleanupOldData(string oldKey)
    {
        try
        {
            if (PlayerPrefs.HasKey(oldKey))
            {
                PlayerPrefs.DeleteKey(oldKey);
                PlayerPrefs.Save();
                Debug.Log($"Old data key '{oldKey}' removed");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to cleanup old data: {e.Message}");
        }
    }

    [System.Serializable]
    private class OldUserWrapper
    {
        public List<UserData> Users = new List<UserData>();
    }

    [System.Serializable]
    private class UserMeta
    {
        public string Username;
        public string UserID;
        public string Name;
        public string Email;
        public string Role;
    }

    [System.Serializable]
    private class UserMetaList
    {
        public List<UserMeta> Users = new List<UserMeta>();
    }

    private class MigrationResult
    {
        public UserMetaList MetaList { get; set; }
        public int MigratedCount { get; set; }

        public MigrationResult()
        {
            MetaList = new UserMetaList();
            MigratedCount = 0;
        }
    }
}