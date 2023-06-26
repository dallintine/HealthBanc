using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IEncryptionService
    {
        (bool, string) DecryptString(string encryptedText);
        string EncryptString(string plainText);
        string GenerateRequestID(string Prefix = "SME");
        string SHA512(string input);
    }
}
