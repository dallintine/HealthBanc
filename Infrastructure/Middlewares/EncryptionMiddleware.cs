using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Middlewares
{
    public class EncryptionMiddleware
    {
        private readonly RequestDelegate _next;

        public EncryptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }
         
        public async Task Invoke(HttpContext httpContext)
        {
            var w = httpContext.Request.Path.Value;
            if (httpContext.Request.Path.Value != null && httpContext.Request.Path.Value.Contains("/api/Encryption/Encrypt"))
            {
                httpContext.Response.Body = EncryptStream(httpContext.Response.Body);
            }
            else if (httpContext.Request.Path.Value != null && httpContext.Request.Path.Value.Contains("/api/Encryption/Decrypt"))
            {
                httpContext.Request.Body = DecryptStream(httpContext.Request.Body);
            }
            else if(httpContext.Request.Path.Value != null && (httpContext.Request.Path.Value.StartsWith("/v1/")))
            {
                httpContext.Response.Body = EncryptStream(httpContext.Response.Body);
                httpContext.Request.Body = DecryptStream(httpContext.Request.Body);
                //if (httpContext.Request.QueryString.HasValue)
                //{
                //    string decryptedString = DecryptString(httpContext.Request.QueryString.Value.Substring(1));
                //    httpContext.Request.QueryString = new QueryString($"?{decryptedString}");
                //}
            }
            await _next(httpContext);
            await httpContext.Request.Body.DisposeAsync();
            await httpContext.Response.Body.DisposeAsync();
        }

        private static CryptoStream EncryptStream(Stream responseStream)
        {
            Aes aes = GetEncryptionAlgorithm();

            ToBase64Transform base64Transform = new ToBase64Transform();
            CryptoStream base64EncodedStream = new CryptoStream(responseStream, base64Transform, CryptoStreamMode.Write);
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            CryptoStream cryptoStream = new CryptoStream(base64EncodedStream, encryptor, CryptoStreamMode.Write);

            return cryptoStream;
        }

        private static Stream DecryptStream(Stream cipherStream)
        {
            Aes aes = GetEncryptionAlgorithm();

            FromBase64Transform base64Transform = new FromBase64Transform(FromBase64TransformMode.IgnoreWhiteSpaces);
            CryptoStream base64DecodedStream = new CryptoStream(cipherStream, base64Transform, CryptoStreamMode.Read);
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            CryptoStream decryptedStream = new CryptoStream(base64DecodedStream, decryptor, CryptoStreamMode.Read);
            return decryptedStream;
        }

        private static Aes GetEncryptionAlgorithm()
        {
            var salt = "1da4f77a96e54490";
            var vector = "$9jYKuTmQ_7g&9+9";

            var key = Encoding.UTF8.GetBytes(salt);
            var iv = Encoding.UTF8.GetBytes(vector);
            Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.FeedbackSize = 128;

            return aes;
        }
    }
}
