using Application.Common.DTO;
using Application.Image.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customer.Commands
{
    public class UpdatePhoneNumberCommand : IRequest<BaseResponse>
    {
        public string PhoneNumber { get; set; }
    }
}
