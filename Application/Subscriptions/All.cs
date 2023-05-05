using Application.CommonDTO;
using AutoMapper;
using DataAccess;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Subscriptions
{
    public class All
    {
        public class Query : IRequest<BaseResponse>
        {
            public AllSubscriptionQuery AllSubscriptionQuery { get; set; }
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
                var paginatedResponse = new PageBaseResponse<List<SubscriptionDTO>>();
                var paginationQuery = request.AllSubscriptionQuery;

                Func<Subscription, bool> query = x =>
                      (string.IsNullOrEmpty(paginationQuery.Status) || x.Status.ToLower() == paginationQuery.Status.ToLower());
                var querySubscriptions = _repositoryWrapper.Subscription.QuerySubscriptions_Plans().Where(query);
                var skip = (paginationQuery.PageNumber - 1) * paginationQuery.PageSize;

                var filteredQueryable = querySubscriptions.Skip(skip).Take(paginationQuery.PageSize).AsQueryable();
                var recordCount = querySubscriptions.Count();
                paginatedResponse.RecordCount = recordCount;
                paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)paginationQuery.PageSize));
                paginatedResponse.PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null;
                paginatedResponse.PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null;
                var subscriptions = await filteredQueryable.ToListAsync(cancellationToken);
                paginatedResponse.Data = _mapper.Map<List<Subscription> , List<SubscriptionDTO>>(subscriptions);
                return paginatedResponse;
            }
        }
    }
}
