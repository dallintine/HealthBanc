using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.General.Interfaces
{
    public interface IOtpValidationRepository : IBaseRepository<OtpValidation>
    {
        Task<OtpValidation> GetUserLastOTP(int userId);
    }
}
