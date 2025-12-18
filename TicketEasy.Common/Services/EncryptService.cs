using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TicketEasy.Services
{
    public class EncryptService
    {
        public static string DecryptText(string cipherText, string? password = null)
        {
            var encryptionPassword = password ?? ConfigProvider.Get().Password;
            return DecryptAES(cipherText, encryptionPassword);
        }

        private static string DecryptAES(string cipherText, string password)
        {
            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);

                using var aes = Aes.Create();

                var key = GenerateKey(password, 32);
                var iv = GenerateKey(password + "salt", 16);

                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                using var msDecrypt = new MemoryStream(cipherBytes);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);

                return srDecrypt.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"AES decryption failed: {ex.Message}", ex);
            }
        }

        private static byte[] GenerateKey(string password, int keySize)
        {
            using var sha256 = SHA256.Create();
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(passwordBytes);

            if (keySize <= hash.Length)
            {
                var key = new byte[keySize];
                Array.Copy(hash, key, keySize);
                return key;
            }
            else
            {
                var key = new byte[keySize];
                var offset = 0;
                while (offset < keySize)
                {
                    var remaining = keySize - offset;
                    var copyLength = Math.Min(remaining, hash.Length);
                    Array.Copy(hash, 0, key, offset, copyLength);
                    offset += copyLength;
                }
                return key;
            }
        }
    }
}
