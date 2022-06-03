using DataAccess.General.Interfaces;
using Domain.Models.Wallet;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.General.Implementation
{
    public class WalletRepository : BaseRepository<Wallet>, IWalletRepository
    {
        public WalletRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
