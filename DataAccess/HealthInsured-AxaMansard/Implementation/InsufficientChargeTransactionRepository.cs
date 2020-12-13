using DataAccess.General.Implementation;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using Domain.Models.AxaMansard_Insurance;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.HealthInsured_AxaMansard.Implementation
{
    class InsufficientChargeTransactionRepository : BaseRepository<InsufficientChargeTransaction>, IInsufficientChargeTransactionRepository
    {
        public InsufficientChargeTransactionRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
