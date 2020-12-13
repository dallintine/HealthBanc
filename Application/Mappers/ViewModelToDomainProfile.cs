using Application.API_RequestModel.HealthInsured_AxaMansard;
using Application.ViewModels;
using Application.ViewModels.AxaMansard;
using AutoMapper;
using Domain.Models;
using Domain.Models.ReportAndLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Mappers
{
    public class ViewModelToDomainProfile : Profile
    {
        public ViewModelToDomainProfile()
        {
            CreateMap<NotificationViewModel, Notification>();
            CreateMap<AuditLogViewModel, UserAuditLog>();
            CreateMap<AdminAuditLogViewModel, AdminAuditLog>();
        }
    }
}
