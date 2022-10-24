using Domain.Models.Wallet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.General.Interfaces
{
    public interface IWalletRepository : IBaseRepository<UserWallet>
    {
        Task<UserWallet> GetByUserId(int userId);
    }
}
