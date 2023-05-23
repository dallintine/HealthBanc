using DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace DataAccess.Implementations
{
    public class TransactionRepository : BaseRepository<Domain.Entities.Transaction>, ITransactionRepository
    {
        public TransactionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Domain.Entities.Transaction> FindByReference( string reference)
        {
            return await _context.Transactions.Include(x => x.Subscriptions).SingleOrDefaultAsync(x => x.Reference == reference);
        }
    }
}
