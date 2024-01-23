using Application.Vendors.Queries;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Vendors.Validators
{
    public class GetVendorPurchaseHistoryValidator : AbstractValidator<GetVendorPurchaseHistory>
    {
        public GetVendorPurchaseHistoryValidator()
        {
            RuleFor(x => x.Id).NotEmpty().NotNull();
        }
    }
}
