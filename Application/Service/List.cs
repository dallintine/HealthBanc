using Application.CommonDTO;
using AutoMapper;
using DataAccess;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Service
{
    public class List
    {
        public class Query : IRequest<BaseResponse> { }
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
                var services = await _repositoryWrapper.Service.FetchServices();
                var serviceDTOs = _mapper.Map<List<Domain.Entities.Service>, List<ServiceDTO>>(services);
                return BaseResponse<List<ServiceDTO>>.Success(serviceDTOs);
            }
        }
    }
}
