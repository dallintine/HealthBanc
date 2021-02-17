using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.AxaMansard_Insurance;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Implementation
{
    public class HygeiaHospitalListRepository : BaseRepository<HygeiaHospitalList>, IHygeiaHospitalListRepository
    {
        public HygeiaHospitalListRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<HygeiaHospitalList>> GetHealthProviders(string state, string city)
        {
            return await _context.HygeiaHospitalLists.Where(x => x.State == state && x.City == city).ToListAsync();
        }

        public IQueryable<HygeiaHospitalList> GetTowns(string state)
        {
            return _context.HygeiaHospitalLists.Where(x => x.State == state);
        }
    }
}
