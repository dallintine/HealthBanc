using AutoMapper;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mappers.Axamnasard_Insurance
{
    public class DomainToDomainProfile : Profile
    {
        public DomainToDomainProfile()
        {
            CreateMap<ApplicationUser, AxaMansardUserProfile>()
               .ForMember(dest => dest.Surname, opt => opt.MapFrom(x => x.LastName))
               .ForMember(dest => dest.Othernames, opt => opt.MapFrom(x => x.FirstName))
               .ForMember(dest => dest.Email, opt => opt.MapFrom(x => x.Email))
               .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(x => x.PhoneNumber));
        }
    }
}
