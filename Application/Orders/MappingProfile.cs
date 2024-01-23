using Application.Orders.DTO;
using AutoMapper;

namespace Application.Orders
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Domain.Entities.Order, OrderDTO>().
                ForMember(dest => dest.FirstName, opt => opt.MapFrom(x => x.ApplicationUser.FirstName)).
                ForMember(dest => dest.LastName, opt => opt.MapFrom(x => x.ApplicationUser.LastName)).
                ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(x => x.ApplicationUser.PhoneNumber)).
                ForMember(dest => dest.Email, opt => opt.MapFrom(x => x.ApplicationUser.Email)).
                ForMember(dest => dest.UserId, opt => opt.MapFrom(x => x.ApplicationUser.Id)).
                ForMember(dest => dest.Plan, opt => opt.MapFrom(x => x.Plan.Name)).
                ForMember(dest => dest.Vendor, opt => opt.MapFrom(x => x.Plan.Vendor.Name));
        }
    }
}
