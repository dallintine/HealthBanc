using HealthBanc.Messages;
using HealthBanc.Types;
using System.Threading.Tasks;

namespace HealthBanc.Dispatchers
{
    public interface IDispatcher
    {
        Task SendAsync<TCommand>(TCommand command) where TCommand : ICommand;
        Task<TResult> QueryAsync<TResult>(IQuery<TResult> query);
    }
}