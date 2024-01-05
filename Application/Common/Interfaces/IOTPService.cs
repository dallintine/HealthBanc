using Application.Common.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IOTPService
    {
        Task<BaseResponse<string>> CreateOTP(long userId, string action);
        Task<BaseResponse> ValidateAdminOTPAuth(string otp, string username);
        Task<BaseResponse> ValidateOTP(long userId, string encryptedOTP, string action);
    }
}
