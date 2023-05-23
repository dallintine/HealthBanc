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
            public long ServiceId { get; set; }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(x => x.ServiceId).NotEmpty().NotNull().Must(x => x > 0).WithMessage("Invalid ProductId");
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
                var plans =  _repositoryWrapper.Plan.QueryPlans(request.ServiceId);
                var planDTOs = await plans.Select(x => new PlanDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Price = x.Price,
                    Discount = x.Discount,
                    VendorName = x.Vendor.Name,
                    ImageURL = x.ImageURL,
                    Tag = x.Tag,
                    ExternalLinkName = x.ExternalLinkName,
                    ExternalLinkURL = x.ExternalLinkURL,
                    OptionalFee = x.OptionalFee,
                    PlanDescriptionDTOs = x.PlanDescriptions.Where(x => x.IsDeleted == false).Select(y => new PlanDescriptionDTO { Id = y.Id, Name = y.Name }).ToList()
                }).ToListAsync(cancellationToken: cancellationToken);
                return BaseResponse<List<PlanDTO>>.Success(planDTOs);
            }
        }
    }
}
