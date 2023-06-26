using Application.Vendors.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Vendors.Validators
{
    public class UpdateVendorCommandValidator : AbstractValidator<UpdateVendorCommand>
    {
        public UpdateVendorCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().NotNull();
            RuleFor(x => x.Name).NotEmpty().NotNull();
            RuleFor(x => x.SettlementAccount).NotEmpty().NotNull().Length(10, 10).Must(x => long.TryParse(x, out var val)).WithMessage("Invalid Settlement Account.");
        }
    }
}
