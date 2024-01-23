using Application.Common.DTO;
using MediatR;

namespace Application.ProspectivePartner.Command
{
    public class CreateProspectivePartnerCommand : IRequest<BaseResponse>
    {
        public string Email { get; set; }
        public string CompanyName { get; set; }
        public string RepresentativeName { get; set; }
        public string PhoneNumber { get; set; }
        public string Location { get; set; }
        public string Category { get; set; }
    }
}
