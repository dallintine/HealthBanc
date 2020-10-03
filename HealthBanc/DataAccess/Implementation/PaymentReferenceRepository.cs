using HealthBanc.Data;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Implementation
{
    public class PaymentReferenceRepository : BaseRepository<PaymentReference>, IPaymentReferenceRepository
    {
        public PaymentReferenceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<PaymentReference> GetActivePayment(int userId)
        {
            return await _context.PaymentReferences.FirstOrDefaultAsync(x => x.UserId == userId && x.Active == true);
        }
    }
}
