using Application.ViewModels.HealthInsured;
using AutoMapper;
using Domain.Models;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.Axa_Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Text;

namespace HealthBanc.Mappers.Insurance
{
    public class ViewModelToDomainProfile : Profile
    {
        public ViewModelToDomainProfile()
        {
            CreateMap<UserProfileviewModel, InsuranceUserProfile>()
                .ForMember(dest => dest.PlanCode, opt => opt.MapFrom(x => x.PlanCode == "7" ? "1" : "2"))
                .ForMember(dest => dest.Premium, opt => opt.MapFrom(x => x.PlanCode == "7" ? Decimal.Parse("1000") : Decimal.Parse("2000")))
                .ForMember(dest => dest.CareProviderName, opt => opt.Ignore())
                .ForMember(dest => dest.DateCreated, opt => opt.MapFrom(x => DateTime.Now));


            CreateMap<UpdateProfileViewModel, InsuranceUserProfile>()
                 .ForMember(dest => dest.PlanCode, opt => opt.MapFrom(x => x.PlanCode == "7" ? "1" : "2"))
                .ForMember(dest => dest.Premium, opt => opt.MapFrom(x => x.PlanCode == "7" ? Decimal.Parse("1000") : Decimal.Parse("2000")))
                .ForMember(dest => dest.CareProviderName, opt => opt.Ignore())
                .ForMember(dest => dest.CPAddress, opt => opt.Ignore());

            CreateMap<FileModel, BeneficiaryReviewUser>();

            CreateMap<FamilyMemberViewModel, InsuranceUserProfile>()
                .ForMember(dest => dest.Surname, opt => opt.MapFrom(x => x.LastName))
                .ForMember(dest => dest.Othernames, opt => opt.MapFrom(x => x.FirstName))
                .ForMember(dest => dest.DateCreated, opt => opt.MapFrom(x => DateTime.Now));

        }
    }
}
