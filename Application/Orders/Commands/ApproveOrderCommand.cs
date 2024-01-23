using Application.Common.DTO;
using MediatR;

namespace Application.Orders.Commands
{
    public class ApproveOrderCommand : IRequest<BaseResponse>
    {
        public long SubscriptionId { get; set; }
    }
}
