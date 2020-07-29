using AutoMapper;
using HealthBanc.Messages.Events;
using HealthBanc.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Mappers
{
    public class ViewModelToMessageProfile : Profile
    {
        public ViewModelToMessageProfile()
        {
            CreateMap<CreateAdminRegViewModel, AdminCreated>();
        }
    }
}
