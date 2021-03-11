using DataAccess.General.Interfaces;
using Domain.Models.Axa_Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Interfaces
{
    public interface IHygeiaHospitalListRepository : IBaseRepository<HygeiaHospitalList>
    {
        Task<PagedResponse<HygeiaHospitalList>> FilterHealthCareProvider(PaginationQuery paginationQuery, string state, string city);
        Task<List<HygeiaHospitalList>> GetHealthProviders(string state, string city);
        IQueryable<HygeiaHospitalList> GetTowns(string state);
    }
}
