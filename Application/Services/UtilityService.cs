using Application.API_ResponseModel.IBSResponse;
using Application.DTO;
using Application.Helpers;
using Application.Interfaces;
using DataAccess;
using Domain.Models;
using Domain.Models.Axa_Hygeia_Insurance;
using Hangfire;
using Hangfire.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UtilityService
    {
        private readonly IRepositoryWrapper _repoWrapper;
        private readonly IBSIntegrationService _iBSIntegrationService;
        private readonly IEmailSender _emailSender;
        private readonly IFileProcessor _fileProcessor;

        private HMOAccountDetails HMOAccountDetails { get; }

        public UtilityService(IRepositoryWrapper repoWrapper, IOptions<HMOAccountDetails> hmoAccountAccessor,IBSIntegrationService iBSIntegrationService,
            IEmailSender emailSender, IFileProcessor fileProcessor)
        {
            _repoWrapper = repoWrapper;
            _iBSIntegrationService = iBSIntegrationService;
            _emailSender = emailSender;
            _fileProcessor = fileProcessor;
            HMOAccountDetails = hmoAccountAccessor.Value;
        }

        public ResponseMessage GetHMOInvoiceDetailsForMonthEnd()
        {
            var hygeiaPayment = _repoWrapper.PaymentReference.QueryAllPaymentReference()
                .Where(x => x.Status.Equals(PaymentReference_StatusValue.Successful.ToString()) && x.Channel.ToLower() == PaymentReference_ChannelValue.healthinsured_hygeia.ToString().ToLower()
                && x.Amount > decimal.Parse("50") && x.Date.Date.Month == DateTime.Now.Date.Month)
                .Select(x => x.Amount).Sum();

            var axaPayment = _repoWrapper.PaymentReference.QueryAllPaymentReference()
                .Where(x => x.Status.Equals(PaymentReference_StatusValue.Successful.ToString()) && x.Channel.ToLower() == PaymentReference_ChannelValue.healthinsured_axamansard.ToString().ToLower()
                && x.Amount > decimal.Parse("50") && x.Date.Date.Month == DateTime.Now.Date.Month)
                .Select(x => x.Amount).Sum();

            var HMOPayment = new
            {
                Hygeia = hygeiaPayment * decimal.Parse("0.9"),
                AxaMansard = axaPayment * decimal.Parse("0.9")
            };
            return new ResponseMessage { Data = HMOPayment, Status = true };
        }

        public async Task<ResponseMessage> UploadAxaHospitalListFromExcel(IFormFile formFile)
        {
            var presentHospitalList = _repoWrapper.AxaMansardHospitalList.GetAll().ToList();
            _repoWrapper.AxaMansardHospitalList.DeleteRange(presentHospitalList);
            await _repoWrapper.Save();
            var hospitalList = await _fileProcessor.UploadAxaHospitalListFromExcel(formFile);
            _repoWrapper.AxaMansardHospitalList.CreateRange(hospitalList);
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true, Message = "Upload was successful" };
        }

        public async Task<ResponseMessage> UploadHygeiaHospitalListFromExcel(IFormFile formFile)
        {
            var presentHospitalList = _repoWrapper.HygeiaHospitalList.GetAll().ToList();
            _repoWrapper.HygeiaHospitalList.DeleteRange(presentHospitalList);
            await _repoWrapper.Save();
            var hospitalList = await _fileProcessor.UploadHygeiaHospitalListFromExcel(formFile);
            _repoWrapper.HygeiaHospitalList.CreateRange(hospitalList);
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true, Message = "Upload was successful" };
        }

    }
}
