using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

internal static class FallbackFileKeyCrypto
{
    private const int keySize = 32;

    public static byte[] Encrypt(byte[] plaintext)
    {
        byte[] key = GetOrCreateKey();

        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.GenerateIV();

            using (var ms = new MemoryStream())
            {
                ms.Write(aes.IV, 0, aes.IV.Length);
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(plaintext, 0, plaintext.Length);
                    cs.FlushFinalBlock();
                }
                return ms.ToArray();
            }
        }
    }

    public static byte[] Decrypt(byte[] data)
    {
        byte[] key = GetOrCreateKey();

        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            byte[] iv = new byte[16];
            Array.Copy(data, 0, iv, 0, 16);
            aes.IV = iv;

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 16, data.Length - 16);
                    cs.FlushFinalBlock();
                }
                return ms.ToArray();
            }
        }
    }

    private static byte[] GetOrCreateKey()
    {
        string folder = Path.Combine(Application.persistentDataPath, "keys");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "metadata.key");

        if (File.Exists(path))
        {
            byte[] existing = File.ReadAllBytes(path);
            if (existing.Length == keySize) return existing;
        }

        byte[] key = new byte[keySize];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(key);

        File.WriteAllBytes(path, key);
        return key;
    }
}