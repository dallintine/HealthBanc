using Application.Common.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Identity.Commands
{
    public class ConfirmEmailCommand : IRequest<BaseResponse>
    {
        public string OTP { get; set; }
        public string Email { get; set; }

    }
}
