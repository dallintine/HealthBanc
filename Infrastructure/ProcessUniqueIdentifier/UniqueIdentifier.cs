using Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.ProcessUniqueIdentifier
{
    public class UniqueIdentifier : IUniqueIdentifier
    {
        public string GetUniqueCode(int nCount)
        {
            string uniqueNumber = string.Empty;
            while (uniqueNumber.Length < nCount)
            {
                string varWW0 = uniqueNumber;
                Guid guid = Guid.NewGuid();
                uniqueNumber = varWW0 + RefineString(guid.ToString().Replace("-", ""));
            }

            if (uniqueNumber.Length > nCount)
            {
                uniqueNumber = uniqueNumber.Substring(0, nCount);
            }
            return uniqueNumber;
        }

        private static string RefineString(string input)
        {
            string str_ = string.Empty;
            char[] arr_ = input.ToCharArray();
            for (int i = 0; i < arr_.Length; i++)
            {
                char chr_ = arr_[i];
                if (char.IsDigit(chr_))
                {
                    str_ += chr_;
                }
            }

            return str_;
        }
    }
}
