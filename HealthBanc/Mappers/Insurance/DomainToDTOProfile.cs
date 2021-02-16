using Application.DTO.HealthInsured_AxaMansard;
using AutoMapper;
using Domain.Models;
using Domain.Models.Axa.Hygeia_Insurance;
using HealthBanc.DTO.HealthInsured_AxaMansard;
using System;
using System.Collections.Generic;
using System.Text;

namespace HealthBanc.Mappers.Insurance
{
    public class DomainToDTOProfile : Profile
    {
        public DomainToDTOProfile()
        {
            CreateMap<InsuranceUserProfile, IndividualProfileDTO>()
               .ForMember(dest => dest.PlanCode, opt => opt.MapFrom(x => x.PlanCode == "1" ? "7" : "8"))
               .ForMember(dest => dest.EnrolleeNumber, opt => opt.MapFrom(x => x.TransId));

            CreateMap<InsuranceUserProfile, InsuranceBeneficiaryDTO>()
               .ForMember(dest => dest.FullName, opt => opt.MapFrom(x => x.Othernames +" "+x.Surname))
               .ForMember(dest => dest.Amount, opt => opt.MapFrom(x => x.Premium));

            CreateMap<CompanyProfile, CompanyProfileDTO>();
        }
    }
}
