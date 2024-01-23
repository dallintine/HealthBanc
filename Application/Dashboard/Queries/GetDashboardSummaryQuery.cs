using Application.Common.DTO;
using Application.Dashboard.DTO;
using MediatR;

namespace Application.Dashboard.Queries
{
    public class GetDashboardSummaryQuery : IRequest<BaseResponse<DashboardSummaryQuery>>
    {
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
