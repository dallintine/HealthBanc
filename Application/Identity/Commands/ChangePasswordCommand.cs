using Application.Common.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Identity.Commands
{
    public class ChangePasswordCommand : IRequest<BaseResponse>
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
