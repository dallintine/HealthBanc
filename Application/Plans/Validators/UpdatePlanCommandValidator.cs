using Application.Plans.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Plans.Validators
{
    public class UpdatePlanCommandValidator : AbstractValidator<UpdatePlanCommand>
    {
        public UpdatePlanCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().NotNull();
            RuleFor(x => x.Id).NotEmpty().NotNull().Must(x => x > 0).WithMessage("Id cannot be zero");
            RuleFor(x => x.Price).NotEmpty().NotNull().Must(x => x > 0).WithMessage("Price has to be greater than zero.");
        }
    }
}
