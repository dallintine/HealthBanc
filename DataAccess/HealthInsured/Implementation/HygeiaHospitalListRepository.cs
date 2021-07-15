using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models.Axa.Hygeia_Insurance;
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
    public class HygeiaHospitalListRepository : BaseRepository<HygeiaHospitalList>, IHygeiaHospitalListRepository
    {
        public HygeiaHospitalListRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<HygeiaHospitalList>> GetHealthProviders(string state, string city)
        {
            var hospitalList = _context.HygeiaHospitalLists.AsQueryable();

            if (state != null)
            {
                hospitalList = hospitalList.Where(x => x.State.ToLower().Trim() == state.ToLower().Trim());
            }
            if (city != null)
            {
                hospitalList = hospitalList.Where(x => x.City.ToLower().Trim() == city.ToLower().Trim());
            }
            var newHospitalList = await hospitalList.ToListAsync();
            return newHospitalList;
        }

        public IQueryable<HygeiaHospitalList> GetTowns(string state)
        {
            return _context.HygeiaHospitalLists.Where(x => x.State.ToLower().Trim() == state.ToLower().Trim());
        }

        public async Task<PagedResponse<HygeiaHospitalList>> FilterHealthCareProvider(PaginationQuery paginationQuery,string state,string city)
        {
            var paginatedResponse = new PagedResponse<HygeiaHospitalList>();
            var queryable = _context.HygeiaHospitalLists.AsQueryable();

            if (!string.IsNullOrEmpty(state))
            {
                queryable = queryable.Where(x => x.State.ToLower().Trim() == state.ToLower().Trim()).AsQueryable();
            }

            if (!string.IsNullOrEmpty(city))
            {
                queryable = queryable.Where(x => x.City.ToLower().Trim() == city.ToLower().Trim()).AsQueryable();
            }

            if (!string.IsNullOrEmpty(paginationQuery.SearchText))
            {
                queryable = queryable.Where(x => x.HospitalName.Contains(paginationQuery.SearchText) || x.City.Contains(paginationQuery.SearchText) ||
                x.State.Contains(paginationQuery.SearchText));
            }

            //Sort the users
            if (string.IsNullOrEmpty(state))
            {
                queryable = queryable.OrderBy(s => s.State);
            }
            if (!string.IsNullOrEmpty(city))
            {
                queryable = queryable.OrderBy(s => s.HospitalName);
            }
            else
            {
                queryable = queryable.OrderBy(s => s.City);
            }

            var skip = (paginationQuery.PageNumber - 1) * paginationQuery.PageSize;
            var newQueryable = queryable.Skip(skip).Take(paginationQuery.PageSize).AsQueryable();
            paginatedResponse.Data = await newQueryable.ToListAsync();
            var recordCount2 = await queryable.CountAsync();
            paginatedResponse.RecordCount = recordCount2;
            paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount2 / (double)paginationQuery.PageSize));
            return paginatedResponse;
        }
    }
}
