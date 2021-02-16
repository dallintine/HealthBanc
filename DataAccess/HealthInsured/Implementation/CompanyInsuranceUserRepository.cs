using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models.Axa.Hygeia_Insurance;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Implementation
{
    public class CompanyInsuranceUserRepository : BaseRepository<CompanyInsuranceUser>, ICompanyInsuranceUserRepository
    {
        public CompanyInsuranceUserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<CompanyInsuranceUser> GetByEmail(string email)
        {
            return await _context.CompanyInsuranceUsers.FirstOrDefaultAsync(x => x.Email == email);
        }
    }
}
