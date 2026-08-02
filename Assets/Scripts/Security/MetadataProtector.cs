using System;
using System.Text;

public static class MetadataProtector
{
    public static string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;

        byte[] bytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] encrypted;

#if UNITY_ANDROID && !UNITY_EDITOR
        encrypted = AndroidKeystoreCrypto.Encrypt(bytes);
#else
        encrypted = FallbackFileKeyCrypto.Encrypt(bytes);
#endif
        return Convert.ToBase64String(encrypted);
    }


    public static bool TryDecrypt(string stored, out string plaintext)
    {
        plaintext = null;
        if (string.IsNullOrEmpty(stored)) return false;

        try
        {
            byte[] encrypted = Convert.FromBase64String(stored);
            byte[] decrypted;

#if UNITY_ANDROID && !UNITY_EDITOR
            decrypted = AndroidKeystoreCrypto.Decrypt(encrypted);
#else
            decrypted = FallbackFileKeyCrypto.Decrypt(encrypted);
#endif
            plaintext = Encoding.UTF8.GetString(decrypted);
            return true;
        }
        catch
        {
            return false;
        }
    }
}