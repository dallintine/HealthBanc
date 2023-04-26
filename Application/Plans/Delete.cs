using Application.CommonDTO;
using DataAccess;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Plans
{
    public class Delete
    {
        public class Command : IRequest<BaseResponse>
        {
            public long Id { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Id).NotEmpty().NotNull();
            }
        }

        public class Handler : IRequestHandler<Command, BaseResponse>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IRepositoryWrapper _repositoryWrapper;

            public Handler(ILogger<Handler> logger, IRepositoryWrapper repositoryWrapper)
            {
                _logger = logger;
                _repositoryWrapper = repositoryWrapper;
            }

            public async Task<BaseResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                _logger.LogInformation($"Delete Plan request processing [PlanId : {request.Id}]");
                var plan = await _repositoryWrapper.Plan.Find(x => x.Id == request.Id && !x.IsDeleted);
                if (plan is null) return BaseResponse.Failure("25", "No record found");
                plan.IsDeleted = true;
                _repositoryWrapper.Plan.Update(plan);
                await _repositoryWrapper.Save();
                return BaseResponse.Success();
            }
        }
    }
}
