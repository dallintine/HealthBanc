using DataAccess.General.Interfaces;
using Domain.Models.Axa_Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Interfaces
{
    public interface IHMOPaymentRepository : IBaseRepository<HMOPayment>
    {
        Task<HMOPayment> GetFailedPaymentById(int id);
        Task<HMOPayment> GetPaymentByJobId(string jobId);
        Task<int> GetPaymentCount(string channel);
    }
}
