using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IWalletEncryptionsAndDecryption
    {
        string Decrypt(string ciphertext);
        string Encrypt(string plaintext);
    }
}
