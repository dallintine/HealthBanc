using Application.ViewModels.HealthInsured;
using AutoMapper;
using Domain.Models;
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
                .ForMember(dest => dest.AlternateHospital, opt => opt.Ignore())
                .ForMember(dest => dest.CustomerPhoto, opt => opt.Ignore())
                .ForMember(dest => dest.IdentityPhoto, opt => opt.Ignore());


            CreateMap<UpdateProfileViewModel, InsuranceUserProfile>()
                 .ForMember(dest => dest.PlanCode, opt => opt.MapFrom(x => x.PlanCode == "7" ? "1" : "2"))
                .ForMember(dest => dest.Premium, opt => opt.MapFrom(x => x.PlanCode == "7" ? Decimal.Parse("1000") : Decimal.Parse("2000")))
                .ForMember(dest => dest.CareProviderName, opt => opt.Ignore())
                .ForMember(dest => dest.CPAddress, opt => opt.Ignore());

            CreateMap<FileModel, InsuranceUserProfile>()
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(x => DateTime.Parse(x.DateOfBirth)))
                .ForMember(dest => dest.Surname, opt => opt.MapFrom(x => x.LastName))
                .ForMember(dest => dest.Othernames, opt => opt.MapFrom(x => x.FirstName))
                .ForMember(dest => dest.ContactAddress, opt => opt.MapFrom(x => x.Address));

            CreateMap<FileModel, ApplicationUser>()
                .ForMember(dest => dest.DateOfRegistration, opt => opt.MapFrom(x => DateTime.Now))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(x => x.Email));
        }
    }
}
