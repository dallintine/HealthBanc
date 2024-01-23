using Application.Vendors.Commands;
using Application.Vendors.Queries;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Vendors.Validators
{
    public class GetVendorQueryValidator : AbstractValidator<GetVendorQuery>
    {
        public GetVendorQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty().NotNull();
        }
    }
}
