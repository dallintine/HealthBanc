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

        public async Task<PaymentReference> GetByUserId(int userId)
        {
            return await _context.PaymentReferences.FirstOrDefaultAsync(x => x.UserId == userId);
        }
        public async Task<PaymentReference> GetById(int id)
        {
            return await _context.PaymentReferences.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PaymentReference> GetByReference(string reference)
        {
            return await _context.PaymentReferences.FirstOrDefaultAsync(x => x.Refernce == reference);
        }
    }
}
