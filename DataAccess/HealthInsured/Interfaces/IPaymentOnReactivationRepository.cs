using DataAccess.General.Interfaces;
using Domain.Models.AxaMansard_Insurance;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Interfaces
{
    public interface IPaymentOnReactivationRepository : IBaseRepository<PaymentOnReactivation>
    {
        Task<PaymentOnReactivation> GetScheduledPaymentByJobId(string jobId, int userId);
    }
}
