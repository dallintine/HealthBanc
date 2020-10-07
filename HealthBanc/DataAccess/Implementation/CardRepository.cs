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

        public async Task<DebitCard> GetCardByIdAsync(int id, int userId)
        {
            return  await _context.Cards.Include(x => x.TokenizationReference).FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        }

        public async Task<int> GetUserCardCount(int userId)
        {
            return await _context.Cards.Where(x => x.UserId == userId).CountAsync();
        }

        public async Task<DebitCard> GetPrimaryCard(int userId)
        {
            return await _context.Cards.FirstOrDefaultAsync(x => x.UserId == userId && x.Status == 1);
        }

        public async Task<TokenizationReference> GetPrimaryCardReference(int id)
        {
            return await _context.Cards.Where(x => x.UserId == id && x.Status == 1).Include(x => x.TokenizationReference).Select(x => x.TokenizationReference).FirstOrDefaultAsync();
        }

        public async Task<DebitCard> CheckIfCardWasPreviouslyTokenized(int id ,string lastFourDigit)
        {
            return await _context.Cards.FirstOrDefaultAsync(x => x.UserId == id && x.LastFourDigit == lastFourDigit);
        }
    }
}
