using Application.DTO;
using Application.Interfaces;
using Application.ViewModels;
using AutoMapper;
using DataAccess;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class LeadGeneratorService
    {
        private readonly IMapper _mapper;
        private readonly IEmailSender _emailSender;
        private readonly IRepositoryWrapper _repoWrapper;

        public LeadGeneratorService(IMapper mapper, IEmailSender emailSender,IRepositoryWrapper repoWrapper)
        {
            _mapper = mapper;
            _emailSender = emailSender;
            _repoWrapper = repoWrapper;
        }

        public async Task<ResponseMessage> SendHealthFinanceData(HealthFinanceCollectionViewModel healthFinance)
        {
            var emails = new List<string>
            {
                "Effiong.Effiong@sterling.ng" , "Hassan.Hassan@sterling.ng"
            };
            _emailSender.SendHealthFinanceNotification("HealthFinance Notification", healthFinance, emails);
            var financeData = _mapper.Map<HealthFinance>(healthFinance);
            _repoWrapper.HealthFinance.Create(financeData);
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true, Message = "Notification was sent successfully" };
        }

        public ResponseMessage SendHeliumNotification(HeliumHealthCollectionViewModel heliumHealth)
        {
            _emailSender.SendHeliumNotification("Helium Notification", heliumHealth.HealthServiceProviderName, heliumHealth.HealthServiveProviderType, heliumHealth.PhoneNumber,
                   heliumHealth.EmailAddress);
            return new ResponseMessage { Status = true, Message = "Notification was sent successfully" };

        }

        public async Task<ResponseMessage> GetPaginatedHealthFinaceData(PaginationQuery paginationQuery)
        {
            var financeData = await _repoWrapper.HealthFinance.GetPaginatedFinanceData(paginationQuery);
            return new ResponseMessage { Data = financeData, Status = true, Message="Data was fetched successfully" };

        }
    }
}
