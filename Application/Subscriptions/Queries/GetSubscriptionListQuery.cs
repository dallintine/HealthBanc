using Application.Common.DTO;
using Application.Common.Query;
using Application.Plans.DTO;
using Application.Subscriptions.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Subscriptions.Queries
{
    public class GetSubscriptionListQuery : PaginationQuery , IRequest<PageBaseResponse<List<SubscriptionDTO>>>
    {
        public string Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

}
