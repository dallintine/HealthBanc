using AutoMapper;
using HealthBanc.Request.AxaMansard;
using HealthBanc.ViewModels;
using HealthBanc.ViewModels.AxaMansard;
using System;
using System.Collections.Generic;
using System.Globalization;
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

            CreateMap<UserProfileviewModel, UserProfile>();

            CreateMap<Card, HealthBanc.Request.Tokenize.Card>();
        }
    }
}
