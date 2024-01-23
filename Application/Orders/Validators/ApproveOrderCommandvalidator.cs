using Application.Orders.Commands;
using FluentValidation;

namespace Application.Orders.Validators
{
    public class ApproveOrderCommandvalidator : AbstractValidator<ApproveOrderCommand>
    {
        public ApproveOrderCommandvalidator()
        {
            RuleFor(x => x.SubscriptionId).NotEmpty().NotNull();
        }
    }
}
