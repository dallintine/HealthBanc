using Application.Common.DTO;
using Application.Common.Query;
using Application.Orders.DTO;
using MediatR;

namespace Application.Orders.Queries
{
    public class GetOrdersListQuery : PaginationQuery , IRequest<PageBaseResponse<List<OrderDTO>>>
    {
        public long? UserId { get; set; }
        public  long? VendorId { get; set; }
        public  long? PlanId { get; set; }
        public string Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

}
