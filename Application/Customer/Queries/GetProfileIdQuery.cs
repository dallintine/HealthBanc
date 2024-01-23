using Application.Common.DTO;
using Application.Customer.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customer.Queries
{
    public class GetProfileIdQuery : IRequest<BaseResponse<CustomerIdDTO>>
    {
    }
}
