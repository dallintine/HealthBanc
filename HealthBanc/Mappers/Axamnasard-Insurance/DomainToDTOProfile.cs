using AutoMapper;
using Domain.Models;
using HealthBanc.DTO.ApplicationUserDTOs;
using HealthBanc.DTO.HealthInsured_AxaMansard;
using System;
using System.Collections.Generic;
using System.Text;

namespace HealthBanc.Mappers.Axamnasard_Insurance
{
    public class DomainToDTOProfile : Profile
    {
        public DomainToDTOProfile()
        {
            CreateMap<AxaMansardUserProfile, AxaMansardUserDTO>()
               .ForMember(dest => dest.PlanCode, opt => opt.MapFrom(x => x.PlanCode == "1" ? "7" : "8"))
               .ForMember(dest => dest.EnrolleeNumber, opt => opt.MapFrom(x => x.TransId));
        }
    }
}
