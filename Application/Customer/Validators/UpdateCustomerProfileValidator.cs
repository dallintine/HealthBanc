using Application.Customer.Commands;
using Application.Identity.Queries;
using FluentValidation;

namespace Application.Customer.Validators
{
    public class UpdateCustomerProfileValidator : AbstractValidator<UpdateCustomerProfile>
    {
        public UpdateCustomerProfileValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty().NotNull();
            RuleFor(x => x.FirstName).NotEmpty().NotNull();
        }
    }
}
