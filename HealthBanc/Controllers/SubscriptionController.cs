namespace HealthBanc.Controllers
{
    public class SubscriptionController : BaseApiController
    {
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(ILogger<SubscriptionController> logger)
        {
            _logger = logger;
        }
    }
}
