using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models;
using Domain.Models.Axa_Hygeia_Insurance;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Implementation
{
    public class HMOPaymentRepository : BaseRepository<HMOPayment>, IHMOPaymentRepository
    {
        public HMOPaymentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<int> GetPaymentCount(string channel)
        {
            var payment = await _context.HMOPayments.Where(x => x.Channel == channel).CountAsync();
            return payment;
        }

        public async Task<HMOPayment> GetPaymentByJobId(string jobId)
        {
            var payment = await _context.HMOPayments.Where(x => x.JobId == jobId).FirstOrDefaultAsync();
            return payment;
        }

        public async Task<List<HMOPayment>> GetPayments()
        {
            var payments = await _context.HMOPayments.ToListAsync();
            return payments;
        }

        public async Task<HMOPayment> GetFailedPaymentById(int id)
        {
            var payment = await _context.HMOPayments.Where(x => x.Id == id && x.Status == PaymentReference_StatusValue.Failed.ToString()).FirstOrDefaultAsync();
            return payment;
        }
    }
}
