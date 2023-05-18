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

namespace Application.Products
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
                var products = await _repositoryWrapper.Product.FetchProducts();
                var productDTOs = _mapper.Map<List<Vendor>, List<ProductDTO>>(products);
                return BaseResponse<List<ProductDTO>>.Success(productDTOs);
            }
        }
    }
}
