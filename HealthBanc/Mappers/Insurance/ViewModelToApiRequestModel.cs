using Application.API_RequestModel.HealthInsured;
using Application.API_RequestModel.Paystack;
using Application.ViewModels.HealthInsured;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace HealthBanc.Mappers.Insurance
{
    public class ViewModelToApiRequestModel : Profile
    {
        public ViewModelToApiRequestModel()
        {
            CreateMap<UserProfileviewModel, EnrollmentModel>()
               .ForMember(dest => dest.Dob, opt => opt.MapFrom(x => x.DateOfBirth))
               .ForMember(dest => dest.State, opt => opt.MapFrom(x => x.StateOfResidence))
               .ForMember(dest => dest.Lga, opt => opt.MapFrom(x => x.TownOfResidence))
               .ForMember(dest => dest.PlanId, opt => opt.MapFrom(x => x.PlanCode == "7" ? "1" : "2"))
               .ForMember(dest => dest.Gender, opt => opt.MapFrom(x => x.Gender == "Male" ? "1" : "2"))
               .ForMember(dest => dest.MaritalStatus, opt => opt.MapFrom(x => x.MaritalStatus == "Single" ? "1" : x.Gender == "Married" ? "2" : "3"))
               .ForMember(dest => dest.Hospital, opt => opt.MapFrom(x => x.CareProviderName));

            CreateMap<Application.ViewModels.Card, Card>();
        }
    }
}
