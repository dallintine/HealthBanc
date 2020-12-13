using Application.API_RequestModel.HealthInsured_AxaMansard;
using Application.DTO;
using Application.ViewModels;
using Application.ViewModels.AxaMansard;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

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
