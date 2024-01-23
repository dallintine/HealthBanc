using Application.Common.DTO;
using Application.Vendors.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Vendors.Queries
{
    public class GetVendorPurchaseHistory : IRequest<BaseResponse<VendorPurchaseHistoryDTO>>
    {
        public long Id { get; set; }
    }
}
