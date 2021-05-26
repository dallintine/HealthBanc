using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models.Axa_Hygeia_Insurance;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Implementation
{
    public class EncryptedAcessTokenRepository : BaseRepository<EncryptedAcessToken>, IEncryptedAcessTokenRepository
    {
        public EncryptedAcessTokenRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<EncryptedAcessToken> GetEncryptedToken()
        {
            var token = await _context.EncryptedAcessTokens.FirstOrDefaultAsync();
            return token;
        }
    }
}
