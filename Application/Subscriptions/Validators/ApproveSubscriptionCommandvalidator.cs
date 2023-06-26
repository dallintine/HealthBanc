using Application.Subscriptions.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Subscriptions.Validators
{
    public class ApproveSubscriptionCommandvalidator : AbstractValidator<ApproveSubscriptionCommand>
    {
        public ApproveSubscriptionCommandvalidator()
        {
            RuleFor(x => x.SubscriptionId).NotEmpty().NotNull();
        }
    }
}
