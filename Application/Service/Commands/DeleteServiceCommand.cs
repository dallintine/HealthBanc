using Application.Common.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Service.Commands
{
    public class DeleteServiceCommand : IRequest<BaseResponse>
    {
        public long Id { get; set; }
    }
}
