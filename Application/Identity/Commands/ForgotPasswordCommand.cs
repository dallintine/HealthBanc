using Application.Common.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Identity.Commands
{
    public class ForgotPasswordCommand : IRequest<BaseResponse>
    {
        public string Email { get; set; }
    }
}
