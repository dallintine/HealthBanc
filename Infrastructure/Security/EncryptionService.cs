using Application.Core.ConfigSettings;
using Application.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Infrastructure.Security
{
    public class EncryptionService : IEncryptionService
    {
        private readonly AppEndpointSettings _appEndpointSettings;

        public EncryptionService(IOptions<AppEndpointSettings> appEndpointSettings)
        {
            _appEndpointSettings = appEndpointSettings.Value;
        }

        /// <summary>
        /// Class to encrypt application credentials
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public string EncryptString(string plainText)
        {
            var salt = _appEndpointSettings.Salt;
            var vector = _appEndpointSettings.VectorKey;

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

        /// <summary>
        /// Decrypt Application Credentials
        /// </summary>
        /// <param name="encryptedText"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public (bool, string) DecryptString(string encryptedText)
        {
            var salt = _appEndpointSettings.Salt;
            var vector = _appEndpointSettings.VectorKey;

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

        public string SHA512(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            using (var hash = System.Security.Cryptography.SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);
                // Convert to text
                // StringBuilder Capacity is 128, because 512 bits / 8 bits in byte * 2 symbols for byte
                var hashedInputStringBuilder = new StringBuilder(128);
                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("x2"));
                return hashedInputStringBuilder.ToString();
            }
        }

        public string GenerateRequestID(string Prefix = "HB")
        {
            StringBuilder builder = new StringBuilder();
            Enumerable
          .Range(65, 26)
           .Select(e => ((char)e).ToString())
           .Concat(Enumerable.Range(97, 26).Select(e => ((char)e).ToString()))
           .Concat(Enumerable.Range(0, 10).Select(e => e.ToString()))
           .OrderBy(e => Guid.NewGuid())
           .Take(13)
           .ToList().ForEach(e => builder.Append(e));
            return Prefix + builder.ToString();
        }
    }
}
