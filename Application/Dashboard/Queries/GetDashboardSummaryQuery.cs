using Application.Common.DTO;
using Application.Dashboard.DTO;
using Application.Subscriptions.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dashboard.Queries
{
    public class GetDashboardSummaryQuery : IRequest<BaseResponse<DashboardSummaryQuery>>
    {
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
