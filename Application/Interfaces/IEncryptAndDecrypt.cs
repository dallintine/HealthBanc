using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IEncryptAndDecrypt
    {
        (bool, string) DecryptString(string encryptedText);
        string EncryptStream(Stream stream);
        string EncryptString(string plainText);
        string Sha512Hash(string value);
    }
}
