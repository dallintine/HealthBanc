using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models.Axa.Hygeia_Insurance;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Implementation
{
    public class CompanyProfileRepository : BaseRepository<CompanyProfile>, ICompanyProfileRepository
    {
        public CompanyProfileRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<CompanyProfile> GetCompanyProfileByEmail(string email)
        {
            return await _context.CompanyProfiles.Include(x => x.Cards).FirstOrDefaultAsync(x => x.CompanyEmail == email);
        }

        public async Task<CompanyProfile> GetCompanyProfileByUserId(int userId)
        {
            return await _context.CompanyProfiles.Include(x => x.Cards).FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<CompanyProfile> GetCompanyBeneficiaryReviewUsersByCompanyId(int companyId)
        {
            return await _context.CompanyProfiles.Include(x => x.BeneficiaryReviewUsers).FirstOrDefaultAsync(x => x.Id == companyId);
        }

        public async Task<CompanyProfile> GetCompanyInsuranceUserProfilesByUserId(int userId)
        {
            return await _context.CompanyProfiles.Include(x => x.Cards).Include(x => x.InsuranceUserProfiles).FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public IQueryable<CompanyProfile> QueryAllCompanyProfiles()
        {
            return _context.CompanyProfiles.AsQueryable();
        }
    }
}
