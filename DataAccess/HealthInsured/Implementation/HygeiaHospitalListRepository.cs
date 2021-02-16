using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models.Axa.Hygeia_Insurance;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.HealthInsured.Implementation
{
    public class HygeiaHospitalListRepository : BaseRepository<HygeiaHospitalList>, IHygeiaHospitalListRepository
    {
        public HygeiaHospitalListRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
