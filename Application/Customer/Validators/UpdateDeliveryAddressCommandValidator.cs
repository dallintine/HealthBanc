using Application.Customer.Commands;
using FluentValidation;

namespace Application.Customer.Validators
{
    public class UpdateDeliveryAddressCommandValidator : AbstractValidator<UpdateDeliveryAddressCommand>
    {
        public UpdateDeliveryAddressCommandValidator()
        {
            RuleFor(x => x.DeliveryAddress).NotEmpty().NotNull();
        }
    }
}
