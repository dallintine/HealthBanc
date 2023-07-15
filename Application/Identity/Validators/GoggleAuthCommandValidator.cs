using Application.Identity.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Identity.Validators
{
    public class GoggleAuthCommandValidator : AbstractValidator<GoggleAuthCommand>
    {
        public GoggleAuthCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().NotNull().EmailAddress();
        }
    }
}
