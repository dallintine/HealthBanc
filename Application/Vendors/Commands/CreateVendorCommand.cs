using Application.Common.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Vendors.Commands
{
    public class CreateVendorCommand : IRequest<BaseResponse>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string SettlementAccount { get; set; }
        public long ServiceId { get; set; }
    }
}
