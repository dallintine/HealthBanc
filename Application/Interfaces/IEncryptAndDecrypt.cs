using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IEncryptAndDecrypt
    {
        string EncryptString(string text, string keyString);
        string DecryptString(string cipherText, string keyString);
    }
}
