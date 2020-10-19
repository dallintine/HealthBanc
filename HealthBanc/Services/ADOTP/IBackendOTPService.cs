using OTPNewReference;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Services.ADOTP
{
    public interface IBackendOTPService
    {
        Task<OtpValidationResponse> OtpValidationAsync(string otp, string username);
        string SOAPManual(string otp, string username);
    }
}
