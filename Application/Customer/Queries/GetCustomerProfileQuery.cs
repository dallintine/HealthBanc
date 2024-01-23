using Application.Common.DTO;
using Application.Customer.DTO;
using MediatR;

namespace Application.Customer.Queries
{
    public class GetCustomerProfileQuery : IRequest<BaseResponse<CustomerProfileDTO>>
    {
    }
}
