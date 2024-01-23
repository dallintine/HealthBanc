using Application.Card.Commands;
using FluentValidation;

namespace Application.Card.Validators
{
    public class DeleteCardCommandValidator : AbstractValidator<DeleteCardCommand>
    {
        public DeleteCardCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().NotNull();
        }
    }
}
