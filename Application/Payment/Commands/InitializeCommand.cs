using Application.Common.DTO;
using Application.Payment.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Payment.Commands
{
    public class InitializeCommand : IRequest<BaseResponse>
    {
        public List<InitializePaymentDTO> InitializePaymentDTOs { get; set; }
        public string Note { get; set; }
    }
}
