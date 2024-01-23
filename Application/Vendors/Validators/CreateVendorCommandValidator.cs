using Application.Vendors.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Vendors.Validators
{
    public class CreateVendorCommandValidator : AbstractValidator<CreateVendorCommand>
    {
        public CreateVendorCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().NotNull();
            RuleFor(x => x.ServiceId).NotEmpty().NotNull();
            RuleFor(x => x.SettlementAccount).Length(10, 10).Must(x => long.TryParse(x, out var val)).WithMessage("Invalid Settlement Account.");
        }
    }
}
