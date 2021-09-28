using Application.ViewModels;
using AutoMapper;
using DataAccess;
using DataAccess.HealthInsured.Interfaces;
using DataAccess.Logs.Interfaces;
using Domain.Models.ReportAndLogs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UAParser;

namespace Application.AuditAndReport.AuditLog
{
    public class AuditLogService
    {
        private readonly IMapper _mapper;
        private readonly IRepositoryWrapper _repoWrapper;

        public AuditLogService(IMapper mapper,IRepositoryWrapper repoWrapper)
        {
            _mapper = mapper;
            _repoWrapper = repoWrapper;
        }

        public async Task UserCreateAuditLog(AuditLogViewModel viewModel, string ip, string device)
        {
            var auditLog = _mapper.Map<UserAuditLog>(viewModel);
            auditLog.IPAddress = ip;
            auditLog.Device = device;
            auditLog.Date = DateTime.Now;
            auditLog.Id = new Guid();
            _repoWrapper.UserAuditLog.Create(auditLog);
            await _repoWrapper.Save();
            await Task.CompletedTask;
        }

        public async Task AdminCreateAuditLog(AdminAuditLogViewModel viewModel)
        {
            var auditLog = _mapper.Map<AdminAuditLog>(viewModel);
            auditLog.Date = DateTime.Now;
            auditLog.Id = new Guid();
            auditLog.IPAddress = "test";
            _repoWrapper.AdminAuditLog.Create(auditLog);
            await _repoWrapper.Save();
            await Task.CompletedTask;
        }

        public string GetDevice(StringValues agent)
        {
            var userAgent = agent;
            string uaString = Convert.ToString(userAgent[0]);
            var uaParser = Parser.GetDefault();
            ClientInfo c = uaParser.Parse(uaString);
            return  c.OS.ToString() + "," + c.UA.ToString() + "," +c.Device.Model+","+c.Device.Brand+","+c.Device.Family;
        }
    }
}