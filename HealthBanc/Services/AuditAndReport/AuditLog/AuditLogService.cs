using HealthBanc.DataAccess.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UAParser;
using Wangkanai.Detection.Services;

namespace HealthBanc.Services.AuditAndReport.AuditLog
{
    public class AuditLogService
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IDetectionService _detection;
        private readonly IAuditLogRepository _auditLogRepository;

        public AuditLogService(IHttpContextAccessor accessor,IDetectionService detection,IAuditLogRepository auditLogRepository)
        {
            _accessor = accessor;
            _detection = detection;
            _auditLogRepository = auditLogRepository;
        }

        public async Task CreateAuditLog(HealthBanc.Domain.Models.ReportAndLogs.UserAuditLog auditLog)
        {
            _auditLogRepository.Create(auditLog);
            await _auditLogRepository.Save();
        }

        public string GetIPAddress()
        {
            var ip = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();
            return ip;
        }

        public string GetDevice()
        {
            var userAgent = _accessor.HttpContext.Request.Headers["User-Agent"];
            string uaString = Convert.ToString(userAgent[0]);
            var uaParser = Parser.GetDefault();
            ClientInfo c = uaParser.Parse(uaString);

            return c.OS.ToString() + " " + c.UA.ToString() + " " + _detection.Device.Type.ToString();
        }
    }
}
