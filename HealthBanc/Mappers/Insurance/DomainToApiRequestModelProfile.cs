using Application.API_RequestModel.HealthInsured;
using AutoMapper;
using Domain.Models;
using Domain.Models.Axa_Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Text;

namespace HealthBanc.Mappers.Insurance
{
    public class DomainToApiRequestModelProfile : Profile
    {
        public DomainToApiRequestModelProfile()
        {
            CreateMap<ApplicationUser, EnrollmentModel>()
               .ForMember(dest => dest.Surname, opt => opt.MapFrom(x => x.LastName))
               .ForMember(dest => dest.FirstName, opt => opt.MapFrom(x => x.FirstName))
               .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(x => ""))
               .ForMember(dest => dest.MobileNo, opt => opt.MapFrom(x => x.PhoneNumber))
               .ForMember(dest => dest.Email, opt => opt.MapFrom(x => x.Email));

            CreateMap<InsuranceUserProfile, EnrollmentModel>()
              .ForMember(dest => dest.Surname, opt => opt.MapFrom(x => x.Surname))
              .ForMember(dest => dest.FirstName, opt => opt.MapFrom(x => x.Othernames))
              .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(x => ""))
              .ForMember(dest => dest.MobileNo, opt => opt.MapFrom(x => x.PhoneNumber))
              .ForMember(dest => dest.Email, opt => opt.MapFrom(x => x.Email))
              .ForMember(dest => dest.Dob, opt => opt.MapFrom(x => x.DateOfBirth))
              .ForMember(dest => dest.EnrollmentNo, opt => opt.MapFrom(x => x.TransId))
              .ForMember(dest => dest.PlanId, opt => opt.MapFrom(x => x.PlanCode))
              .ForMember(dest => dest.Gender, opt => opt.MapFrom(x => x.Gender == "Male" ? "1" : "2"))
              .ForMember(dest => dest.MaritalStatus, opt => opt.MapFrom(x => x.MaritalStatus == "Single" ? "1" : x.Gender == "Married" ? "2" : "3"))
              .ForMember(dest => dest.State, opt => opt.MapFrom(x => x.StateOfResidence))
              .ForMember(dest => dest.Hospital, opt => opt.MapFrom(x => x.CareProviderName))
              .ForMember(dest => dest.Lga, opt => opt.MapFrom(x => x.TownOfResidence));

            CreateMap<InsuranceUserProfile, RegistrationModel>()
               .ForMember(dest => dest.LastName, opt => opt.MapFrom(x => x.Surname))
              .ForMember(dest => dest.FirstName, opt => opt.MapFrom(x => x.Othernames))
              .ForMember(dest => dest.Phone, opt => opt.MapFrom(x => x.PhoneNumber))
              .ForMember(dest => dest.Email, opt => opt.MapFrom(x => x.Email))
              .ForMember(dest => dest.DOB, opt => opt.MapFrom(x => x.DateOfBirth))
              .ForMember(dest => dest.GenderId, opt => opt.MapFrom(x => x.Gender == "Male" ? "1" : "2"))
              .ForMember(dest => dest.Address, opt => opt.MapFrom(x => x.ContactAddress))
              .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(x => x.Image));

        }
    }
}
