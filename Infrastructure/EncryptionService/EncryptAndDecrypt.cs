using Application.Helpers.ThirdPartyAPI;
using Application.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Infrastructure.EncryptionService
{
    public class EncryptAndDecrypt : IEncryptAndDecrypt
    {
        private readonly AppEndpoint _optionAccessor;

        public EncryptAndDecrypt(IOptions<AppEndpoint> optionAccessor)
        {
            _optionAccessor = optionAccessor.Value;
        }
        public string EncryptString(string plainText)
        {
            var salt = _optionAccessor.APIUri.Salt;
            var vector = _optionAccessor.APIUri.VectorKey;

            var key = Encoding.UTF8.GetBytes(salt);
            var iv = Encoding.UTF8.GetBytes(vector);

            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
            {
                throw new ArgumentNullException("plainText");
            }
            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }
            if (iv == null || iv.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }
            byte[] encrypted;
            // Create a RijndaelManaged object
            // with the specified key and IV.
            using (var rijAlg = new RijndaelManaged())
            {
                rijAlg.Mode = CipherMode.CBC;
                rijAlg.Padding = PaddingMode.PKCS7;
                rijAlg.FeedbackSize = 128;

                rijAlg.Key = key;
                rijAlg.IV = iv;

                // Create a decrytor to perform the stream transform.
                var encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption.
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            // Return the encrypted bytes from the memory stream.
            return Convert.ToBase64String(encrypted);
        }

        public string EncryptStream(Stream stream)
        {
            var salt = _optionAccessor.APIUri.Salt;
            var vector = _optionAccessor.APIUri.VectorKey;

            var key = Encoding.UTF8.GetBytes(salt);
            var iv = Encoding.UTF8.GetBytes(vector);

            // Check arguments.
            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }
            if (iv == null || iv.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }
            byte[] encrypted;
            // Create a RijndaelManaged object
            // with the specified key and IV.
            using (var rijAlg = new RijndaelManaged())
            {
                rijAlg.Mode = CipherMode.CBC;
                rijAlg.Padding = PaddingMode.PKCS7;
                rijAlg.FeedbackSize = 128;

                rijAlg.Key = key;
                rijAlg.IV = iv;

                // Create a decrytor to perform the stream transform.
                var encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption.
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        stream.CopyTo(csEncrypt);
                        stream.Close();
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            // Return the encrypted bytes from the memory stream.
            return Convert.ToBase64String(encrypted);
        }

        public (bool, string) DecryptString(string encryptedText)
        {
            var salt = _optionAccessor.APIUri.Salt;
            var vector = _optionAccessor.APIUri.VectorKey;

            var key = Encoding.UTF8.GetBytes(salt);
            var iv = Encoding.UTF8.GetBytes(vector);

            if (!IsBase64String(encryptedText)) return (false, "The Encrypted string is not base64 encoded");
            var cipherText = Convert.FromBase64String(encryptedText);

            if (cipherText == null || cipherText.Length <= 0)
            {
                throw new ArgumentNullException("cipherText");
            }

            string plaintext = null;

            using (var rijAlg = new RijndaelManaged())
            {
                rijAlg.Mode = CipherMode.CBC;
                rijAlg.Padding = PaddingMode.PKCS7;
                rijAlg.FeedbackSize = 128;
                rijAlg.Key = key;
                rijAlg.IV = iv;

                var decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);
                try
                {
                    using (var msDecrypt = new MemoryStream(cipherText))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                plaintext = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
                catch
                {
                    plaintext = "The Encrypted string is not valid";
                    return (false, plaintext);
                }
            }
            return (true, plaintext);
        }

        private static RijndaelManaged NewRijndaelManaged()
        {

            return new RijndaelManaged()
            {
                KeySize = 192,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };

        }

        private static bool IsBase64String(string base64String)
        {
            base64String = base64String.Trim();
            return base64String.Length % 4 == 0 &&
            Regex.IsMatch(base64String, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);
        }

        public string Sha512Hash(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            using (var hash = SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);
                var hashedInputStringBuilder = new StringBuilder(128);
                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("x2"));
                return hashedInputStringBuilder.ToString().ToUpper(); ;
            }
        }
    }
}
