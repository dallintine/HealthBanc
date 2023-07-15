using Application.Payment.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Payment.CommandValidators
{
    public class CallbackCommandValidator : AbstractValidator<CallbackCommand>
    {
        public CallbackCommandValidator()
        {
            RuleFor(x => x.Reference).NotEmpty().NotNull();
        }
    }
}
