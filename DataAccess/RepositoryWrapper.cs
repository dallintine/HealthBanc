using Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private ApplicationDbContext _context;
        //public IWalletRepository _wallet;

        //public IWalletRepository Wallet
        //{
        //    get
        //    {
        //        if (_wallet == null)
        //        {
        //            _wallet = new WalletRepository(_context);
        //        }
        //        return _wallet;
        //    }
        //}

        public RepositoryWrapper(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<int> Save()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
