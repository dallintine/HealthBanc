using Application.Common.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Plans.Commands
{
    public class CreatePlanCommand : IRequest<BaseResponse>
    {
        public long ServiceId { get; set; }
        public long VendorId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public double MarkUpRate { get; set; }
    }
}
