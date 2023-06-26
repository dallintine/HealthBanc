using Application.Common.DTO;
using Application.Subscriptions.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Subscriptions.Queries
{
    public class GetPaymentSummaryQuery : IRequest<BaseResponse<PaymentSummaryDTO>>
    {
    }
}
