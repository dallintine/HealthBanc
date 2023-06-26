using Application.Vendors.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Vendors.Validators
{
    public class DeleteVendorCommandValidator : AbstractValidator<DeleteVendorCommand>
    {
        public DeleteVendorCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().NotNull();
        }
    }
}
