using AutoMapper;
using HealthBanc.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static HealthBanc.Infrastructure.Mail.EmailSender;

namespace HealthBanc.Mappers
{
    public class ViewModelToDTOProfile : Profile
    {
        public ViewModelToDTOProfile()
        {
            CreateMap<HeliumHealthCollectionViewModel, HelloEmail>();
        }
    }
}
