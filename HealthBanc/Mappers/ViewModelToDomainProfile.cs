using AutoMapper;
using HealthBanc.Domain.Models;
using HealthBanc.ViewModels;
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
        }
    }
}
