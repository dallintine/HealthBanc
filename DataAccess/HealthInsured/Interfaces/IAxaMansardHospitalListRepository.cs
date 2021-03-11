using DataAccess.General.Interfaces;
using Domain.Models.Axa_Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Interfaces
{
    public interface IAxaMansardHospitalListRepository : IBaseRepository<AxaMansardHospitalList>
    {
        Task<List<AxaMansardHospitalList>> GetHealthProviders(string state, string town);
        IQueryable<AxaMansardHospitalList> GetTowns(string state);
    }
}
