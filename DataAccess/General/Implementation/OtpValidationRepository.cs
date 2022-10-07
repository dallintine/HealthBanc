using DataAccess.General.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.General.Implementation
{
    public class OtpValidationRepository : BaseRepository<OtpValidation>, IOtpValidationRepository
    {
        public OtpValidationRepository(ApplicationDbContext context) : base(context)
        {
        }
        public async Task<OtpValidation> GetUserLastOTP(int userId)
        {
            // LiNQ cant evaluate lastordefault hence this logic
            return await _context.OtpValidations.Where(x => x.ApplicationUserId == userId && x.Status == true).OrderByDescending(x => x.GeneratedDate).FirstOrDefaultAsync();
        }
    }
}
