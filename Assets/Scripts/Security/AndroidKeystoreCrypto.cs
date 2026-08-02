#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;

internal static class AndroidKeystoreCrypto
{
    private const string KeyAlias = "mindtune_metadata_key_v1";
    private const string Provider = "AndroidKeyStore";
    private const string Transformation = "AES/GCM/NoPadding";
    private const int GcmTagBits = 128;

    public static byte[] Encrypt(byte[] plaintext)
    {
        EnsureKeyExists();

        using (var cipher = GetCipher())
        using (var secretKey = GetSecretKey())
        {
            cipher.Call("init", 1 /* Cipher.ENCRYPT_MODE */, secretKey);
            byte[] iv = cipher.Call<byte[]>("getIV");
            byte[] ciphertext = cipher.Call<byte[]>("doFinal", plaintext);

            byte[] result = new byte[4 + iv.Length + ciphertext.Length];
            Array.Copy(BitConverter.GetBytes(iv.Length), 0, result, 0, 4);
            Array.Copy(iv, 0, result, 4, iv.Length);
            Array.Copy(ciphertext, 0, result, 4 + iv.Length, ciphertext.Length);
            return result;
        }
    }

    public static byte[] Decrypt(byte[] data)
    {
        int ivLen = BitConverter.ToInt32(data, 0);
        byte[] iv = new byte[ivLen];
        Array.Copy(data, 4, iv, 0, ivLen);
        byte[] ciphertext = new byte[data.Length - 4 - ivLen];
        Array.Copy(data, 4 + ivLen, ciphertext, 0, ciphertext.Length);

        using (var cipher = GetCipher())
        using (var secretKey = GetSecretKey())
        using (var gcmSpec = new AndroidJavaObject("javax.crypto.spec.GCMParameterSpec", GcmTagBits, iv))
        {
            cipher.Call("init", 2 /* Cipher.DECRYPT_MODE */, secretKey, gcmSpec);
            return cipher.Call<byte[]>("doFinal", ciphertext);
        }
    }

    private static void EnsureKeyExists()
    {
        using (var ksClass = new AndroidJavaClass("java.security.KeyStore"))
        {
            var ks = ksClass.CallStatic<AndroidJavaObject>("getInstance", Provider);
            ks.Call("load", null);
            if (ks.Call<bool>("containsAlias", KeyAlias)) return;
        }

        using (var kgClass = new AndroidJavaClass("javax.crypto.KeyGenerator"))
        using (var kg = kgClass.CallStatic<AndroidJavaObject>("getInstance", "AES", Provider))
        using (var propsClass = new AndroidJavaClass("android.security.keystore.KeyProperties"))
        {
            int purposes = propsClass.GetStatic<int>("PURPOSE_ENCRYPT") | propsClass.GetStatic<int>("PURPOSE_DECRYPT");

            using (var builder = new AndroidJavaObject(
                "android.security.keystore.KeyGenParameterSpec$Builder", KeyAlias, purposes))
            {
                builder.Call<AndroidJavaObject>("setBlockModes", new string[] { "GCM" });
                builder.Call<AndroidJavaObject>("setEncryptionPaddings", new string[] { "NoPadding" });
                builder.Call<AndroidJavaObject>("setKeySize", 256);

                using (var spec = builder.Call<AndroidJavaObject>("build"))
                {
                    kg.Call("init", spec);
                }
            }
            kg.Call<AndroidJavaObject>("generateKey");
        }
    }

    private static AndroidJavaObject GetCipher()
    {
        var cipherClass = new AndroidJavaClass("javax.crypto.Cipher");
        return cipherClass.CallStatic<AndroidJavaObject>("getInstance", Transformation);
    }

    private static AndroidJavaObject GetSecretKey()
    {
        using (var ksClass = new AndroidJavaClass("java.security.KeyStore"))
        {
            var ks = ksClass.CallStatic<AndroidJavaObject>("getInstance", Provider);
            ks.Call("load", null);
            var entry = ks.Call<AndroidJavaObject>("getEntry", KeyAlias, null);
            return entry.Call<AndroidJavaObject>("getSecretKey");
        }
    }
}
#endif