using AutoMapper;
using Domain.Models;
using Domain.Models.Axa.Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Text;

namespace HealthBanc.Mappers.Insurance
{
    public class DomainToDomainProfile : Profile
    {
        public DomainToDomainProfile()
        {
            CreateMap<ApplicationUser, InsuranceUserProfile>()
               .ForMember(dest => dest.Surname, opt => opt.MapFrom(x => x.LastName))
               .ForMember(dest => dest.Othernames, opt => opt.MapFrom(x => x.FirstName))
               .ForMember(dest => dest.Email, opt => opt.MapFrom(x => x.Email))
               .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(x => x.PhoneNumber))
               .ForMember(dest => dest.Id, opt => opt.Ignore());


            CreateMap<CompanyInsuranceUser, ApplicationUser>()
                .ForMember(dest => dest.DateOfRegistration, opt => opt.MapFrom(x => DateTime.Now))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(x => x.Email));

            CreateMap<CompanyInsuranceUser, InsuranceUserProfile>()
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(x => DateTime.Parse(x.DateOfBirth)))
                .ForMember(dest => dest.Surname, opt => opt.MapFrom(x => x.LastName))
                .ForMember(dest => dest.Othernames, opt => opt.MapFrom(x => x.FirstName))
                .ForMember(dest => dest.DateCreated, opt => opt.MapFrom(x => DateTime.Now))
                .ForMember(dest => dest.SubscriptionStatus, opt => opt.MapFrom(x => true))
                .ForMember(dest => dest.StartActiveStatusDate, opt => opt.MapFrom(x => DateTime.Now))
                .ForMember(dest => dest.EndActiveStatusDate, opt => opt.MapFrom(x => DateTime.Now))
                .ForMember(dest => dest.ActiveStatus, opt => opt.MapFrom(x => true))
                .ForMember(dest => dest.ContactAddress, opt => opt.MapFrom(x => x.Address));
        }
    }
}
