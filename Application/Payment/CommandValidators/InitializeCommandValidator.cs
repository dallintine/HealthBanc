using Application.Payment.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Payment.CommandValidators
{
    public class InitializeCommandValidator : AbstractValidator<InitializeCommand>
    {
        public InitializeCommandValidator()
        {
            RuleFor(x => x.InitializePaymentDTOs).NotNull().NotEmpty();
        }
    }
}
