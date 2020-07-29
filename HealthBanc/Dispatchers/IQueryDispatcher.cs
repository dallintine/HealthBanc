using HealthBanc.Types;
using System.Threading.Tasks;

namespace HealthBanc.Dispatchers
{
    public interface IQueryDispatcher
    {
        Task<TResult> QueryAsync<TResult>(IQuery<TResult> query);
    }
}