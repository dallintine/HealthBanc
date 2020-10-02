using HealthBanc.Data;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
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
    }
}
