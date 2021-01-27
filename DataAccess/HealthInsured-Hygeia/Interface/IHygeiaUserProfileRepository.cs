using Domain.Models.Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured_Hygeia.Interface
{
    public interface IHygeiaUserProfileRepository
    {
        Task<HygeiaUserProfile> GetByUserIdAsync(int id);
    }
}
