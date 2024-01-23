using Application.Common.DTO;
using MediatR;

namespace Application.Orders.Queries
{
    public class ExportPaymentList : IRequest<BaseResponse<byte[]>>
    {
        public string Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
