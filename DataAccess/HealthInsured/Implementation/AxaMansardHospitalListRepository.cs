using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
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
    public class AxaMansardHospitalListRepository : BaseRepository<AxaMansardHospitalList>, IAxaMansardHospitalListRepository
    {
        public AxaMansardHospitalListRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<AxaMansardHospitalList>> GetHealthProviders(string state, string city)
        {
            var hospitalList = _context.AxaMansardHospitalLists.AsQueryable();

            if(state != null)
            {
                hospitalList = hospitalList.Where(x => x.State.ToLower().Trim() == state.ToLower().Trim());
            }
            if(city != null)
            {
                hospitalList = hospitalList.Where(x => x.City.ToLower().Trim() == city.ToLower().Trim());
            }
            var newHospitalList = await hospitalList.ToListAsync();
            return newHospitalList;
        }

        public IQueryable<AxaMansardHospitalList> GetTowns(string state)
        {
            return  _context.AxaMansardHospitalLists.Where(x => x.State.ToLower().Trim() == state.ToLower().Trim());
        }
    }
}
