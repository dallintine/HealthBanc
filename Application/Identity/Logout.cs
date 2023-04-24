using Application.CommonDTO;
using Application.Interfaces;
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
                var userId = _tokenService.GetClaims().FirstOrDefault(x => x.Type == "UserId")?.Value;
                await _tokenService.ClearSession(long.Parse(userId));
                return BaseResponse.Success();
            }
        }
    }
}
