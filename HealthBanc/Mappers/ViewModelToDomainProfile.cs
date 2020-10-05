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
            CreateMap<UserProfileviewModel, iHealth>();
            CreateMap<iHealth, AxaMansardUserProfile>();
            CreateMap<UpdateProfileViewModel, AxaMansardUserProfile>();
        }
    }
}
