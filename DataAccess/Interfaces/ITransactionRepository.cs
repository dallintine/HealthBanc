using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace DataAccess.Interfaces
{
    public interface ITransactionRepository : IBaseRepository<Domain.Entities.Transaction>
    {
        Task<Domain.Entities.Transaction> FindByReference(string reference);
    }
}
