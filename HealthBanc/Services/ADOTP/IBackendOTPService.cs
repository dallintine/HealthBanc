using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Services.ADOTP
{
    public interface IBackendOTPService
    {
        string SOAPManual(string otp, string username);
    }
}
