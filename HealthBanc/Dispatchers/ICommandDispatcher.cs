using HealthBanc.Messages;
using System.Threading.Tasks;

namespace HealthBanc.Dispatchers
{
    public interface ICommandDispatcher
    {
         Task SendAsync<T>(T command) where T : ICommand;
    }
}