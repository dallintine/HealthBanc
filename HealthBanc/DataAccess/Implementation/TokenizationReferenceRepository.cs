using HealthBanc.Data;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Implementation
{
    public class TokenizationReferenceRepository : BaseRepository<TokenizationReference>, ITokenizationReferenceRepository
    {
        public TokenizationReferenceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<TokenizationReference> GetBySuperAdminIdAsync(int superAdminId)
        {
            return await _context.TokenizationReferences.FirstOrDefaultAsync(x => x.SuperAdminId == superAdminId);
        }
    }
}
