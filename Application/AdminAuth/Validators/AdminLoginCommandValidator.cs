using Application.AdminAuth.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.AdminAuth.Validators
{
    public class AdminLoginCommandValidator : AbstractValidator<AdminLoginCommand>
    {
        public AdminLoginCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().NotNull().EmailAddress();
            RuleFor(x => x.OTP).NotEmpty().NotNull().Length(6, 6).WithMessage("OTP must be 6 characters").Must(x => long.TryParse(x, out var val)).WithMessage("OTP must be numeric characters."); ;
            RuleFor(x => x.Password).NotEmpty().NotNull();
        }
    }
}
