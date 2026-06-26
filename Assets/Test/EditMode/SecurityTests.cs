using NUnit.Framework;
using UnityEngine;
using System;

public class SecurityTests
{
    private const string TestUserId = "test-user-123";
    private const string TestPassword = "MyTestP@ssw0rd!";


    [Test]
    public void PasswordHasher_ValidPassword_ReturnsTrue()
    {
        string password = TestPassword;
        string hash = PasswordHasher.HashPassword(password);

        bool isValid = PasswordHasher.VerifyPassword(password, hash);

        Assert.IsTrue(isValid, "Верный пароль должен пройти проверку.");
    }

    [Test]
    public void PasswordHasher_InvalidPassword_ReturnsFalse()
    {
        string password = TestPassword;
        string hash = PasswordHasher.HashPassword(password);
        string wrongPassword = "WrongP@ssw0rd!";

        bool isValid = PasswordHasher.VerifyPassword(wrongPassword, hash);

        Assert.IsFalse(isValid, "Неверный пароль НЕ должен проходить проверку.");
    }

    [Test]
    public void PasswordHasher_EmptyPassword_ReturnsFalse()
    {
        bool isValid = PasswordHasher.VerifyPassword("", "somehash");

        Assert.IsFalse(isValid, "Пустой пароль не должен проходить проверку.");
    }

    [Test]
    public void PasswordHasher_NullPassword_ReturnsFalse()
    {
        bool isValid = PasswordHasher.VerifyPassword(null, "somehash");

        Assert.IsFalse(isValid, "Null пароль не должен проходить проверку.");
    }

    [Test]
    public void PasswordHasher_InvalidHash_ReturnsFalse()
    {
        bool isValid = PasswordHasher.VerifyPassword(TestPassword, "invalidhash");

        Assert.IsFalse(isValid, "Неверный хеш должен возвращать false.");
    }

    [Test]
    public void SecureStorage_RoundTrip_SavesAndLoadsDataCorrectly()
    {
        var testData = new TestData
        {
            Name = "Test User",
            Value = 42,
            IsActive = true
        };

        SecureStorage.SaveEncryptedData(testData, TestUserId);

        TestData loadedData = SecureStorage.LoadEncryptedData<TestData>(TestUserId);

        Assert.IsNotNull(loadedData, "Загруженные данные не должны быть null.");
        Assert.AreEqual(testData.Name, loadedData.Name, "Имя должно совпадать.");
        Assert.AreEqual(testData.Value, loadedData.Value, "Значение должно совпадать.");
        Assert.AreEqual(testData.IsActive, loadedData.IsActive, "Флаг должен совпадать.");
    }

    [Test]
    public void SecureStorage_RoundTrip_HandlesEmptyData()
    {
        var testData = new TestData();

        SecureStorage.SaveEncryptedData(testData, TestUserId);
        TestData loadedData = SecureStorage.LoadEncryptedData<TestData>(TestUserId);

        Assert.IsNotNull(loadedData, "Даже пустые данные должны загружаться.");
        Assert.IsNull(loadedData.Name, "Имя должно быть null.");
        Assert.AreEqual(0, loadedData.Value, "Значение должно быть 0.");
        Assert.IsFalse(loadedData.IsActive, "Флаг должен быть false.");
    }

    [Test]
    public void SecureStorage_LoadNonExistentUser_ReturnsDefault()
    {
        var loadedData = SecureStorage.LoadEncryptedData<TestData>("non-existent-user");

        Assert.IsNotNull(loadedData, "Для несуществующего пользователя должен возвращаться новый объект.");
        Assert.IsNull(loadedData.Name, "Данные должны быть пустыми.");
    }

    [Test]
    public void SecureStorage_RoundTrip_HandlesSpecialCharacters()
    {
        var testData = new TestData
        {
            Name = "Тестовый Пользователь!@#$%^&*()",
            Value = -100,
            IsActive = false
        };

        SecureStorage.SaveEncryptedData(testData, TestUserId);
        TestData loadedData = SecureStorage.LoadEncryptedData<TestData>(TestUserId);

        Assert.IsNotNull(loadedData, "Данные не должны быть null.");
        Assert.AreEqual(testData.Name, loadedData.Name, "Специальные символы должны сохраняться.");
        Assert.AreEqual(testData.Value, loadedData.Value, "Отрицательные числа должны сохраняться.");
        Assert.AreEqual(testData.IsActive, loadedData.IsActive, "Булевы значения должны сохраняться.");
    }

    [Serializable]
    private class TestData
    {
        public string Name;
        public int Value;
        public bool IsActive;
    }

    [SetUp]
    public void Setup()
    {
        string folder = System.IO.Path.Combine(Application.persistentDataPath, "userdata");
        string filePath = System.IO.Path.Combine(folder, $"{TestUserId}.enc");
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
    }

    [TearDown]
    public void Cleanup()
    {
        SecureStorage.DeleteUserData(TestUserId);
    }
}