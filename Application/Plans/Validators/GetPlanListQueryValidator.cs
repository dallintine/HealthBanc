using Application.Plans.Queries;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Plans.Validators
{
    public class GetPlanListQueryValidator : AbstractValidator<GetPlanListQuery>
    {
        public GetPlanListQueryValidator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().NotNull().Must(x => x > 0).WithMessage("Invalid ProductId");
        }
    }
}
