using Application.Common.DTO;
using MediatR;

namespace Application.Customer.Commands
{
    public class UpdateDeliveryAddressCommand : IRequest<BaseResponse>
    {
        public string DeliveryAddress { get; set; }
    }
}
