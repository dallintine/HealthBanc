using Application.Common.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Identity.Commands
{
    public class ResetPasswordCommand : IRequest<BaseResponse>
    {
        public string Email { get; set; }
        public string EmailToken { get; set; }
        public string Password { get; set; }
    }
}
