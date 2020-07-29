using Autofac;
using HealthBanc.Handlers;
using HealthBanc.Messages;
using HealthBanc.RabbitMq;
using System.Threading.Tasks;

namespace HealthBanc.Dispatchers
{
    public class CommandDispatcher : ICommandDispatcher
    {
        private readonly IComponentContext _context;

        public CommandDispatcher(IComponentContext context)
        {
            _context = context;
        }

        public async Task SendAsync<T>(T command) where T : ICommand
            => await _context.Resolve<ICommandHandler<T>>().HandleAsync(command, CorrelationContext.Empty);
    }
}