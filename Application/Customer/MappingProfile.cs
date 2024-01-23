using Application.Customer.DTO;
using Application.Payment.DTO;
using AutoMapper;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customer
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ApplicationUser, CustomerProfileDTO>().
                ForMember(dest => dest.ReferralCode, opt => opt.MapFrom(x => x.Id.ToString().PadLeft(6, '0'))).
                ForMember(dest => dest.HealthDetailDTO, opt => opt.MapFrom(x => x.HealthDetail)).
                ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(x => x.PhoneNumber));

            CreateMap<HealthDetail, HealthDetailDTO>();
        }
    }
}
