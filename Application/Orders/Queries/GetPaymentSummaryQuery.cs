using Application.Common.DTO;
using Application.Orders.DTO;
using MediatR;

namespace Application.Orders.Queries
{
    public class GetPaymentSummaryQuery : IRequest<BaseResponse<PaymentSummaryDTO>>
    {
    }
}
