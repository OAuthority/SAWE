using System.Security.Cryptography;
using System.Text;
using Functions.SecureStorage;

namespace Functions;

public static class EncryptionHelper
{
    /// <summary>
    /// Generate a random, 32 byte encryption key
    /// </summary>
    /// <returns></returns>
    public static byte[] GenerateEncryptionKey()
    {
        byte[] key = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }
        
        return key;
    }

    /// <summary>
    /// This function encrypts a plain string (passwords in the sense of AWBv2.
    /// Each password gets its own IV which is stored alongside the password in the database
    /// since the IV isn't secret, there's no issue here. We return both the password and the IV back to the caller.
    /// Note, this encrypts the password rather than hashes it; which is similar to what AWB does originally. The encryption key
    /// should be stored in ISecureStorage. We cannot directly encrypt the password as encryption is a one-way operation,
    /// we wouldn't be able to get the password back UNLESS the user provides it to check if it matches.
    ///
    /// Pywikibot stores the password in a plain text file so this is inherintly more secure than pywikibot even if hashing
    /// would be the preferred way to do things.
    /// </summary>
    /// <param name="plainText"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static (byte[] CipherText, byte[] IV) Encrypt(string plainText, byte[] key)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                cs.Write(plainBytes, 0, plainBytes.Length);
                cs.FlushFinalBlock();
                return (ms.ToArray(), aes.IV); 
            }
        }
    }

    public static void SaveEncryptionKey(byte[] key)
    {
        if (Globals.IsMac)
        {
            var keychain = new MacOSKeychain();
            keychain.AddPassword("AWBv2", "AWBv2EncryptionKey", key);
        }
    }

    public static byte[]? GetEncryptionKey()
    {
        if (Globals.IsMac)
        {
            var keychain = new MacOSKeychain();
            return keychain.FindPassword("AWBv2", "AWBv2EncryptionKey");
        }

        return null;
    }
    
    public static string Decrypt(byte[] cipherText, byte[] iv, byte[] key)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using (MemoryStream ms = new MemoryStream(cipherText))
            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (StreamReader sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }

}