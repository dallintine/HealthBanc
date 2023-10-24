using Application.Common.DTO;
using MediatR;

namespace Application.Plans.Commands
{
    public class DeletePlanDescriptionCommand : IRequest<BaseResponse>
    {
        public long Id { get; set; }
    }
}
