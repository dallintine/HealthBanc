using Application.ViewModels.AxaMansard;
using AutoMapper;
using Domain.Models;
using Domain.Models.Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Mappers.Hygeia_Insurance
{
    public class ViewModelToDomainProfile : Profile
    {
        public ViewModelToDomainProfile()
        {
            CreateMap<UserProfileviewModel, HygeiaUserProfile>()
                .ForMember(dest => dest.PlanCode, opt => opt.Ignore())
                .ForMember(dest => dest.Premium, opt => opt.MapFrom(x => x.PlanCode == "7" ? Decimal.Parse("1000") : Decimal.Parse("2000")))
                .ForMember(dest => dest.CareProviderName, opt => opt.Ignore())
                .ForMember(dest => dest.CustomerPhoto, opt => opt.Ignore())
                .ForMember(dest => dest.IdentityPhoto, opt => opt.Ignore());
        }
    }
}
