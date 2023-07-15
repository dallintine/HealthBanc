using Application.Common.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Identity.Queries
{
    public class RefreshTokenQuery : IRequest<BaseResponse>
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }
}
