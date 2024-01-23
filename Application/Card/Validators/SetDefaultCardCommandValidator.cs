using Application.Card.Commands;
using FluentValidation;

namespace Application.Card.Validators
{
    public class SetDefaultCardCommandValidator : AbstractValidator<SetDefaultCardCommand>
    {
        public SetDefaultCardCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().NotNull();
        }
    }
}
