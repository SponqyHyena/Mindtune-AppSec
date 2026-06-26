using System;
using System.Collections.Generic;
using UnityEngine;

public class UserManager : MonoBehaviour
{
    private Dictionary<string, UserData> usersCache = new Dictionary<string, UserData>();

    private static readonly string USERS_LIST_KEY = StorageKeys.UsersList;
    public static UserData CurrentUser;

    public enum RegistrationError
    {
        Success = 0,
        InvalidName = 1,
        InvalidEmail = 2,
        InvalidUsername = 3,
        InvalidPassword = 4,
        UsernameTaken = 5,
        UnknownError = 99
    }

    private void Awake()
    {
        StorageKeys.Initialize();
        SecureStorage.MigrateOldData();
        LoadAllUsers();
    }

    private void LoadAllUsers()
    {
        if (!PlayerPrefs.HasKey(USERS_LIST_KEY)) return;

        try
        {
            string json = PlayerPrefs.GetString(USERS_LIST_KEY);

            var userMetaList = SecureJsonSerializer.Deserialize<UserMetaList>(json);

            if (userMetaList != null && userMetaList.Users != null)
            {
                foreach (var meta in userMetaList.Users)
                {
                    if (string.IsNullOrEmpty(meta.Username)) continue;

                    if (!usersCache.ContainsKey(meta.Username))
                    {
                        var dummyUser = new UserData
                        {
                            Name = meta.Name ?? "",
                            Email = meta.Email ?? "",
                            Username = meta.Username,
                            Role = meta.Role ?? "User",
                            UserID = meta.UserID ?? Guid.NewGuid().ToString(),
                            PasswordHash = ""
                        };
                        usersCache[meta.Username] = dummyUser;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"LoadAllUsers error: {e.Message}");
        }
    }

    public RegistrationError RegisterWithError(string name, string email, string username, string password, string role)
    {
        if (!InputValidator.IsValidName(name))
        {
            Debug.LogError("Invalid name");
            return RegistrationError.InvalidName;
        }

        if (!InputValidator.IsValidEmail(email))
        {
            Debug.LogError("Invalid email");
            return RegistrationError.InvalidEmail;
        }

        if (!InputValidator.IsValidUsername(username))
        {
            Debug.LogError("Invalid username");
            return RegistrationError.InvalidUsername;
        }

        if (!InputValidator.IsValidPassword(password))
        {
            Debug.LogError("Invalid password");
            return RegistrationError.InvalidPassword;
        }

        string normalizedUsername = username.ToLowerInvariant().Trim();
        if (usersCache.ContainsKey(normalizedUsername))
        {
            Debug.LogError($"Username '{normalizedUsername}' already exists");
            return RegistrationError.UsernameTaken;
        }

        try
        {
            string passwordHash = PasswordHasher.HashPassword(password);
            role = string.IsNullOrEmpty(role) ? "User" : role;

            UserData newUser = new UserData(name, email, normalizedUsername, passwordHash, role);
            usersCache[normalizedUsername] = newUser;

            SecureStorage.SaveEncryptedData(newUser, newUser.UserID);
            SaveUsersList();

            return RegistrationError.Success;
        }
        catch (Exception e)
        {
            Debug.LogError($"Registration failed: {e.Message}");
            return RegistrationError.UnknownError;
        }
    }

    public bool Register(string name, string email, string username, string password, string role)
    {
        return RegisterWithError(name, email, username, password, role) == RegistrationError.Success;
    }

    public bool IsUsernameTaken(string username)
    {
        if (string.IsNullOrEmpty(username)) return false;
        string normalizedUsername = username.ToLowerInvariant().Trim();
        return usersCache.ContainsKey(normalizedUsername);
    }

    public bool Login(string username, string password)
    {
        string normalizedUsername = username.ToLowerInvariant().Trim();

        if (string.IsNullOrEmpty(normalizedUsername))
        {
            Debug.LogError("Username is empty");
            return false;
        }

        if (!usersCache.ContainsKey(normalizedUsername))
        {
            Debug.LogError($"User {normalizedUsername} not found");
            return false;
        }

        string userId = usersCache[normalizedUsername].UserID;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("User ID is null or empty");
            return false;
        }

        UserData user = SecureStorage.LoadEncryptedData<UserData>(userId);
        if (user == null || string.IsNullOrEmpty(user.UserID))
        {
            Debug.LogError("Failed to load user data");
            return false;
        }

        usersCache[normalizedUsername] = user;

        if (PasswordHasher.VerifyPassword(password, user.PasswordHash))
        {
            CurrentUser = user;
            return true;
        }

        Debug.LogError("Invalid password");
        return false;
    }

    private void SaveUsersList()
    {
        var metaList = new UserMetaList();
        foreach (var kvp in usersCache)
        {
            var user = kvp.Value;
            if (user == null) continue;

            metaList.Users.Add(new UserMeta
            {
                Username = user.Username ?? "",
                UserID = user.UserID ?? "",
                Name = user.Name ?? "",
                Email = user.Email ?? "",
                Role = user.Role ?? "User"
            });
        }

        string json = SecureJsonSerializer.Serialize(metaList);
        if (!string.IsNullOrEmpty(json))
        {
            PlayerPrefs.SetString(USERS_LIST_KEY, json);
            PlayerPrefs.Save();
        }
    }

    public void ChangePassword(UserData user, string newPassword)
    {
        if (!InputValidator.IsValidPassword(newPassword))
        {
            Debug.LogError("Invalid password format");
            return;
        }

        if (user == null)
        {
            Debug.LogError("User is null");
            return;
        }

        user.PasswordHash = PasswordHasher.HashPassword(newPassword);
        SaveUsersData();
    }

    public void SaveUsersData()
    {
        if (CurrentUser == null) return;
        SecureStorage.SaveEncryptedData(CurrentUser, CurrentUser.UserID);
        SaveUsersList();
    }

    public void Logout()
    {
        CurrentUser = null;
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

        public UserMetaList()
        {
            Users = new List<UserMeta>();
        }
    }
}