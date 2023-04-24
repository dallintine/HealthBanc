using Application.CommonDTO;
using AutoMapper;
using DataAccess;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Plans
{
    public class List
    {
        public class Query : IRequest<BaseResponse>
        {
            public long ProductId { get; set; }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(x => x.ProductId).NotEmpty().NotNull().Must(x => x > 0).WithMessage("Invalid ProductId");
            }
        }

        public class Handler : IRequestHandler<Query, BaseResponse>
        {
            private readonly IRepositoryWrapper _repositoryWrapper;
            private readonly IMapper _mapper;

            public Handler(IRepositoryWrapper repositoryWrapper, IMapper mapper)
            {
                _repositoryWrapper = repositoryWrapper;
                _mapper = mapper;
            }

            public async Task<BaseResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var plans = await _repositoryWrapper.Plan.FetchPlans(request.ProductId);
                var planDTOs = _mapper.Map<List<Plan>, List<PlanDTO>>(plans);
                return BaseResponse<List<PlanDTO>>.Success(planDTOs);
            }
        }
    }
}
