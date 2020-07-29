
using HealthBanc.Messages;
using HealthBanc.RabbitMq;
using System.Threading.Tasks;

namespace HealthBanc.Handlers
{
    public interface IEventHandler<in TEvent> where TEvent : IEvent
    {
        Task HandleAsync(TEvent @event, ICorrelationContext context);
    }
}