using Application.Card.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Card.Validators
{
    public class AddCardCommandValidator : AbstractValidator<AddCardCommand>
    {
        public AddCardCommandValidator()
        {
            RuleFor(x => x.Reference).NotEmpty().NotNull();
        }
    }
}
