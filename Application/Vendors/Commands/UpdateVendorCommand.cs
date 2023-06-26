using Application.Common.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Vendors.Commands
{
    public class UpdateVendorCommand : IRequest<BaseResponse>
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string SettlementAccount { get; set; }
    }
}
