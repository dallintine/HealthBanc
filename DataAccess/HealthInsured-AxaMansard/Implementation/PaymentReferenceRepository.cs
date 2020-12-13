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
    public class PaymentReferenceRepository : BaseRepository<PaymentReference>, IPaymentReferenceRepository
    {
        public PaymentReferenceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<PaymentReference> GetActivePayment(int userId)
        {
            return await _context.PaymentReferences.FirstOrDefaultAsync(x => x.UserId == userId && x.Active == true);
        }
        public async Task<PaymentReference> GetById(int id)
        {
            return await _context.PaymentReferences.FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
