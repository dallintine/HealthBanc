using HealthBanc.Types;
using System.Threading.Tasks;

namespace HealthBanc.Handlers
{
    public interface IQueryHandler<TQuery,TResult> where TQuery : IQuery<TResult>
    {
        Task<TResult> HandleAsync(TQuery query);
    }
}