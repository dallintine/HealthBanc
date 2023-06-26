using Application.Common.DTO;
using Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Identity
{
    public class Logout
    {
        public class Query : IRequest<BaseResponse>
        {
        }

        public class Handler : IRequestHandler<Query, BaseResponse>
        {
            private readonly ITokenService _tokenService;

            public Handler( ITokenService tokenService)
            {
                _tokenService = tokenService;
            }
            public async Task<BaseResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                await _tokenService.ClearSession();
                return BaseResponse.Success();
            }
        }
    }
}
