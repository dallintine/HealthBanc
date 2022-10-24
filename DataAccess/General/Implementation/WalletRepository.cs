using DataAccess.General.Interfaces;
using Domain.Models.Wallet;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.General.Implementation
{
    public class WalletRepository : BaseRepository<UserWallet>, IWalletRepository
    {
        public WalletRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<UserWallet> GetByUserId (int userId)
        {
            var wallet = await _context.Wallets.SingleOrDefaultAsync(x => x.UserId == userId);
            return wallet;
        }
    }
}
