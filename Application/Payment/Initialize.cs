using Application.CommonDTO;
using DataAccess;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Payment
{
    public class Initialize
    {
        public class Query : IRequest<BaseResponse>
        {
            public List<long> PlanIds { get; set; }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(x => x.PlanIds).NotNull().NotEmpty();
            }
        }

        public class Handler : IRequestHandler<Query, BaseResponse>
        {
            private readonly ILogger<Handler> _logger;
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly IRepositoryWrapper _repositoryWrapper;

            public Handler(ILogger<Handler> logger, UserManager<ApplicationUser> userManager,IRepositoryWrapper repositoryWrapper)
            {
                _logger = logger;
                _userManager = userManager;
                _repositoryWrapper = repositoryWrapper;
            }
            public async Task<BaseResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var planList = new List<Plan>();

                // Chek that planm id is correct
                foreach (var item in request.PlanIds)
                {
                    var plan = await _repositoryWrapper.Plan.Find(x => x.Id == item);
                    if (plan is null) return BaseResponse.Failure("25", "No plan record found");
                    planList.Add(plan);
                }
                // Check for multiple plans of the same product
                var groupedPlanList = planList.GroupBy(x => x.ProductId);
                if (groupedPlanList.Count() != planList.Count())
                {
                    return BaseResponse.Failure("06", "Plans of the same product cannot be purchased");
                }
                // calculate total sum
                var totalAmount = planList.Sum(x => (x.Price - x.Discount));
            }
        }
    }
}
