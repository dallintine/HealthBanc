using AutoMapper;
using AxaMansardSoap;
using HealthBanc.Domain.Models;
using HealthBanc.Request.AxaMansard;
using HealthBanc.ViewModels;
using HealthBanc.ViewModels.AxaMansard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Mappers
{
    public class ViewModelToDomainProfile : Profile
    {
        public ViewModelToDomainProfile()
        {
            CreateMap<NotificationViewModel, Notification>();
            CreateMap<UserProfile, AxaMansardUserProfile>();
            CreateMap<UserProfileviewModel, iHealth>()
                  .ForMember(dest => dest.CareProviderName, opt => opt.Ignore())
                  .ForMember(dest => dest.AlternateHospital, opt => opt.Ignore())
                  .ForMember(dest => dest.CPCity, opt => opt.MapFrom(x => x.TownOfResidence))
                  .ForMember(dest => dest.CPEmail, opt => opt.MapFrom(x => "Not Available"))
                  .ForMember(dest => dest.CPPhone, opt => opt.MapFrom(x => "Not Available"));
            CreateMap<iHealth, AxaMansardUserProfile>()
                .ForMember(dest => dest.CustomerPhoto, opt => opt.MapFrom(x => x.CustomerPhoto))
                .ForMember(dest => dest.IdentityPhoto, opt => opt.MapFrom(x => x.IdentityPhoto));
            CreateMap<UpdateProfileViewModel, AxaMansardUserProfile>();
        }
    }
}
