using AutoMapper;
using Domain.Models;
using Domain.Models.Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Mappers.Hygeia_Insurance
{
    public class DomainToDomainProfile : Profile
    {
        public DomainToDomainProfile()
        {
            CreateMap<ApplicationUser, HygeiaUserProfile>()
               .ForMember(dest => dest.Surname, opt => opt.MapFrom(x => x.LastName))
               .ForMember(dest => dest.Othernames, opt => opt.MapFrom(x => x.FirstName))
               .ForMember(dest => dest.Email, opt => opt.MapFrom(x => x.Email))
               .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(x => x.PhoneNumber))
               .ForMember(dest => dest.Id, opt => opt.Ignore());
        }
    }
}
