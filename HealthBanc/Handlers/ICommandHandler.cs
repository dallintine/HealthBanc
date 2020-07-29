using HealthBanc.Messages;
using HealthBanc.RabbitMq;
using System.Threading.Tasks;

namespace HealthBanc.Handlers
{
    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        Task HandleAsync(TCommand command, ICorrelationContext context);
    }
}