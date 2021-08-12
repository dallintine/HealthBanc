using Application.DTO;
using Application.DTO.HealthInsured_AxaMansard;
using AutoMapper;
using Domain.Models;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.Axa_Hygeia_Insurance;
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
               .ForMember(dest => dest.EnrolleeNumber, opt => opt.MapFrom(x => x.TransId))
               .ForPath(dest => dest.CardDTOs, opt => opt.MapFrom(x => x.Cards))
               .ForPath(dest => dest.TransactionLogDTOs, opt => opt.MapFrom(x => x.PaymentReferences));

            CreateMap<InsuranceUserProfile, InsuranceBeneficiaryDTO>()
               .ForMember(dest => dest.FullName, opt => opt.MapFrom(x => x.Othernames +" "+x.Surname))
               .ForMember(dest => dest.EnrolleNumber, opt => opt.MapFrom(x => x.TransId))
               .ForMember(dest => dest.Amount, opt => opt.MapFrom(x => x.Premium));

            CreateMap<CompanyProfile, CompanyProfileDTO>();

            CreateMap<BeneficiaryReviewUser, BeneficiaryReviewDTO>();

            CreateMap<PaymentReference, TransactionLogDTO>()
               .ForMember(dest => dest.FullName, opt => opt.MapFrom(x => x.InsuranceUserProfileId == null ? x.CompanyProfile.CompanyName : x.InsuranceUserProfile.Othernames));

            CreateMap<InsuranceUserProfile, TransactionLogDTO>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(x => x.Surname + " " + x.Othernames));

            CreateMap<InsuranceUserProfile, FamilyMembersDTO>();

            //CreateMap<PaymentReference, TransactionLogDTO>().IncludeMembers(x => x.InsuranceUserProfile).IncludeMembers(x => x.CompanyProfile);
        }
    }
}
