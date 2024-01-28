using Application.Customer.Commands;
using FluentValidation;

namespace Application.Customer.Validators
{
    public class UpdatePhoneNumberCommandValidator : AbstractValidator<UpdatePhoneNumberCommand>
    {
        public UpdatePhoneNumberCommandValidator()
        {
            RuleFor(x => x.PhoneNumber).NotEmpty().NotNull();
        }
    }
}

