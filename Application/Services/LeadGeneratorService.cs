using Application.DTO;
using Application.Interfaces;
using Application.ViewModels;
using AutoMapper;
using ClosedXML.Excel;
using DataAccess;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                "oluwaseunayo.lojede@sterling.ng" , "Esther.kerry@sterling.ng","Opeyemi.adebola@sterling.ng","healthbanc@sterling.ng"
                //"hassan.hassan@sterling.ng","healthbanc@sterling.ng"
            };

            var user = await _repoWrapper.HealthFinance.GetByEmailOrPhoneNumber(healthFinance.Email,healthFinance.Phonenumber);
            if(user is null)
            {
                healthFinance.Amount = healthFinance.Amount[1..];
                var financeData = _mapper.Map<HealthFinance>(healthFinance);
                _repoWrapper.HealthFinance.Create(financeData);
                await _repoWrapper.Save();

                await _emailSender.SendHealthFinanceNotification("HealthFinance Notification", healthFinance, emails);
                await _emailSender.SendHealthFinanceSubmissionNotification("HealthFinance Notification", healthFinance, healthFinance.Email);

                return new ResponseMessage { Status = true, Message = "Thank you! We will reach out to you soon" };
            }
            return new ResponseMessage { Status = false, Message = "Your details was submitted previously. We will contact you shortly" };
            
        }
        
        public async Task<ResponseMessage> SendHeliumNotification(HeliumHealthCollectionViewModel heliumHealth)
        {
            await _emailSender.SendHeliumNotification("Helium Notification", heliumHealth.HealthServiceProviderName, heliumHealth.HealthServiveProviderType, heliumHealth.PhoneNumber,
                   heliumHealth.EmailAddress);
            return new ResponseMessage { Status = true, Message = "Notification was sent successfully" };

        }

        public async Task<ResponseMessage> GetPaginatedHealthFinanceData(PaginationQuery paginationQuery,DateTime? startDate, DateTime? endDate)
        {
            var financeData = await _repoWrapper.HealthFinance.GetPaginatedFinanceData(paginationQuery,startDate,endDate);
            return new ResponseMessage { Data = financeData, Status = true, Message="Data was fetched successfully" };

        }

        public ResponseMessage DowloadFinaceExcelData(DateTime? startDate, DateTime? endDate)
        {
            var data =_repoWrapper.HealthFinance.QueryFinanceData();
            if( startDate != null)
            {
                data = data.Where(x => x.DateSubmitted >= startDate);
            }
            if (endDate != null)
            {
                data = data.Where(x => x.DateSubmitted <= endDate);
            }
            var dataCount = data.ToList().Count;
            var count = 3;
            if (dataCount < 1) return new ResponseMessage { Message = "Count has to be larger than zero" };
            using (var workbook = new XLWorkbook())
            {
                IXLWorksheet worksheet =
                workbook.Worksheets.Add("HealthFinance");

                worksheet.Cell(2, 1).Value = "Name";
                worksheet.Cell(2, 2).Value = "Email";
                worksheet.Cell(2, 3).Value = "Business Name";
                worksheet.Cell(2, 4).Value = "Business Type";
                worksheet.Cell(2, 5).Value = "Business Address";
                worksheet.Cell(2, 6).Value = "Phonenumber";
                worksheet.Cell(2, 7).Value = "Amount ";
                worksheet.Cell(2, 8).Value = "Commented";
                worksheet.Cell(2, 9).Value = "Date";        

                foreach (var item in data.ToList())
                {
                    worksheet.Cell(count, 1).Value = item.Name;
                    worksheet.Cell(count, 2).Value = item.Email;
                    worksheet.Cell(count, 3).Value = item.BusinessName;
                    worksheet.Cell(count, 4).Value = item.BusinessType;
                    worksheet.Cell(count, 5).Value = item.BusinessAddress;
                    worksheet.Cell(count, 6).Value = item.Phonenumber;
                    worksheet.Cell(count, 7).Value = item.Amount.ToString();
                    worksheet.Cell(count, 8).Value = item.Comment;
                    worksheet.Cell(count, 9).Value = item.DateSubmitted.Date.ToShortDateString();
                    count++;
                }
                worksheet.Rows().AdjustToContents();
                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return new ResponseMessage { Data = content, Status = true,Message="Excel data was fetched successfully" };
                }
            }
        }
    }
}
