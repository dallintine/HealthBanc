using HealthBanc.DataAccess.Implementation;
using HealthBanc.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Interfaces
{
    public interface IPaymentReferenceRepository : IBaseRepository<PaymentReference>
    {
        Task<PaymentReference> GetActivePayment(int userId);
    }
}
