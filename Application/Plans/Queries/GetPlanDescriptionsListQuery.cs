using Application.Common.DTO;
using Application.Common.Query;
using Application.Plans.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Plans.Queries
{
    public class GetPlanDescriptionsListQuery : PaginationQuery, IRequest<PageBaseResponse<List<PlanDescriptionDTO>>>
    {
        public long PlanId { get; set; }
    }
}
