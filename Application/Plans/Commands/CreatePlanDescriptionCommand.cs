using Application.Common.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Plans.Commands
{
    public class CreatePlanDescriptionCommand : IRequest<BaseResponse>
    {
        public string Name { get; set; }
        public long PlanId { get; set; }
    }
}
