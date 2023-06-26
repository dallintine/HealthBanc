using Application.Common.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Subscriptions.Commands
{
    public class ApproveSubscriptionCommand : IRequest<BaseResponse>
    {
        public long SubscriptionId { get; set; }
    }
}
