using DataAccess.General.Implementation;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured_AxaMansard.Implementation
{
    public class AxaMansardUserProfileRepository : BaseRepository<AxaMansardUserProfile>, IAxaMansardUserProfileRepository
    {
        public AxaMansardUserProfileRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<AxaMansardUserProfile> GetByUserIdAsync(int id)
        {
            return await _context.AxaMansardUserProfile.Include(x => x.Cards).FirstOrDefaultAsync(x => x.UserId == id);
        }

        public async Task<AxaMansardUserProfile> GetByIdAsync(int id)
        {
            return await _context.AxaMansardUserProfile.Include(x => x.Cards).FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PaymentReference> ActivePaymentReference(int id)
        {
            var user = await _context.AxaMansardUserProfile.Include(x => x.PaymentReferences)
                .FirstOrDefaultAsync(x => x.UserId == id);
            var paymentRefence = user.PaymentReferences.Where(x => x.Active == true).FirstOrDefault();
            return paymentRefence;
        }

        public async Task<AxaMansardUserProfile> UserAndPaymentReference(int id)
        {
            var user = await _context.AxaMansardUserProfile.Include(x => x.PaymentReferences)
               .FirstOrDefaultAsync(x => x.UserId == id);
            return user;
        }
    }
}
