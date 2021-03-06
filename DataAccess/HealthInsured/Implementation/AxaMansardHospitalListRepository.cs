using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
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
    public class AxaMansardHospitalListRepository : BaseRepository<AxaMansardHospitalList>, IAxaMansardHospitalListRepository
    {
        public AxaMansardHospitalListRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<AxaMansardHospitalList>> GetHealthProviders(string state, string city)
        {
            return await _context.AxaMansardHospitalLists.Where(x => x.State.ToLower().Trim() == state.ToLower().Trim() && x.City.ToLower().Trim() == city.ToLower().Trim())
                .ToListAsync();
        }

        public IQueryable<AxaMansardHospitalList> GetTowns(string state)
        {
            return  _context.AxaMansardHospitalLists.Where(x => x.State.ToLower().Trim() == state.ToLower().Trim());
        }
    }
}
