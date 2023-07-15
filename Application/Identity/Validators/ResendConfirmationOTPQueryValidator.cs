using Application.Identity.Queries;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Identity.Validators
{
    public class ResendConfirmationOTPQueryValidator : AbstractValidator<ResendConfirmationOTPQuery>
    {
        public ResendConfirmationOTPQueryValidator()
        {
            RuleFor(x => x.Email).NotEmpty().NotNull().EmailAddress();
        }
    }
}
