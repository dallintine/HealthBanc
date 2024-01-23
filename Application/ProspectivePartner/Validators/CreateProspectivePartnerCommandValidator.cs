using Application.Payment.Commands;
using Application.ProspectivePartner.Command;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.ProspectivePartner.Validators
{
    internal class CreateProspectivePartnerCommandValidator : AbstractValidator<CreateProspectivePartnerCommand>
    {
        public CreateProspectivePartnerCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().NotNull().MaximumLength(50);
            RuleFor(x => x.CompanyName).NotEmpty().NotNull().MaximumLength(100);
            RuleFor(x => x.RepresentativeName).NotEmpty().NotNull().MaximumLength(100);
            RuleFor(x => x.PhoneNumber).NotEmpty().NotNull().MaximumLength(20);
            RuleFor(x => x.Category).NotEmpty().NotNull().MaximumLength(100);
            RuleFor(x => x.Location).NotEmpty().NotNull().MaximumLength(500);
        }
    }
}