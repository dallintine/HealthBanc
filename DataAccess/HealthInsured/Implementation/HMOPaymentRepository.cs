using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models.Axa_Hygeia_Insurance;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.HealthInsured.Implementation
{
    public class HMOPaymentRepository : BaseRepository<HMOPayment>, IHMOPaymentRepository
    {
        public HMOPaymentRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
