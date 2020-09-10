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
    public class CardRepository : BaseRepository<DebitCard>, ICardRepository
    {
        public CardRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<TokenizationReference> GetPrimaryCard(int id)
        {
            return await _context.Cards.Where(x => x.UserId == id && x.Status == 1).Include(x => x.TokenizationReference).Select(x => x.TokenizationReference).FirstOrDefaultAsync();
        }
    }
}
