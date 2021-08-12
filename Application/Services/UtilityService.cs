using Application.API_ResponseModel.IBSResponse;
using Application.DTO;
using Application.Helpers;
using Application.Interfaces;
using DataAccess;
using Domain.Models;
using Domain.Models.Axa_Hygeia_Insurance;
using Hangfire;
using Hangfire.Server;
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

        private HMOAccountDetails HMOAccountDetails { get; }

        public UtilityService(IRepositoryWrapper repoWrapper, IOptions<HMOAccountDetails> hmoAccountAccessor,IBSIntegrationService iBSIntegrationService,
            IEmailSender emailSender)
        {
            _repoWrapper = repoWrapper;
            _iBSIntegrationService = iBSIntegrationService;
            _emailSender = emailSender;
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

        public async Task MakeHygeiaHMOPayment()
        {
            var paymentCount = await _repoWrapper.HMOPayment.GetPaymentCount(PaymentReference_ChannelValue.healthinsured_hygeia.ToString());
            if (paymentCount < 1)
            {
                var healthInsuredAcc = HMOAccountDetails.HealthInsuredAccountNumber;
                var hygeiaAcc = HMOAccountDetails.HygeiaAccountNumber;

                var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

                var endOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                var endOfNextMonth = firstDayOfMonth.AddMonths(2).AddDays(-1);
                var hmoPayment = new HMOPayment()
                {
                    PaymentDate = endOfMonth,
                    NextMonthPaymentDate = endOfNextMonth,
                    Channel = PaymentReference_ChannelValue.healthinsured_hygeia.ToString(),
                    Status = PaymentReference_StatusValue.Pending.ToString(),
                };
                var jobId = BackgroundJob.Schedule(() => ProcessHygeiaHMOPayment(healthInsuredAcc, hygeiaAcc, null, null, null), endOfMonth);
                hmoPayment.JobId = jobId;
                _repoWrapper.HMOPayment.Create(hmoPayment);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
        }

        public async Task ProcessHygeiaHMOPayment(string fromAccount, string toAccount, decimal? amount, string getJobId, PerformContext context)
        {
            decimal hygeiaPayment;
            if (amount is null)
            {
                hygeiaPayment = _repoWrapper.PaymentReference.QueryAllPaymentReference()
               .Where(x => x.Status.Equals(PaymentReference_StatusValue.Successful.ToString()) && x.Channel.ToLower() == PaymentReference_ChannelValue.healthinsured_hygeia.ToString().ToLower()
               && x.Amount > decimal.Parse("50") && x.Date.Date.Month == DateTime.Now.Date.Month)
               .Select(x => x.Amount).Sum();

                hygeiaPayment *= decimal.Parse("0.9");
            }
            else
            {
                hygeiaPayment = amount.Value;
            }

            string jobId;
            if (getJobId == null)
            {
                jobId = context.BackgroundJob.Id;
            }
            else
            {
                jobId = getJobId;
            }

            var pendingPayment = await _repoWrapper.HMOPayment.GetPaymentByJobId(jobId);
            var hygeiaPaymentResponse = new OBJ_IBS_Transfer_Response_Class();
            if (hygeiaPayment > 50)
            {
                hygeiaPaymentResponse = await _iBSIntegrationService.SterlingBankIntraBank(hygeiaPayment, toAccount,
               fromAccount, "Automated Sterling Bank monthly payment to Hygiea. HealthBanc- Hygeia partnership ", "NG0020032");
            }
            else
            {
                hygeiaPaymentResponse.ResponseCode = "00";
                hygeiaPaymentResponse.Reference = Guid.NewGuid().ToString();
                hygeiaPaymentResponse.FTReference = "00";
            }

            pendingPayment.Reference = hygeiaPaymentResponse.Reference;
            pendingPayment.PaymentDate = DateTime.Now;
            pendingPayment.Amount = hygeiaPayment;

            if (hygeiaPaymentResponse.ResponseCode == "00")
            {
                pendingPayment.Status = PaymentReference_StatusValue.Successful.ToString();
                pendingPayment.FTReference = hygeiaPaymentResponse.FTReference;
                _repoWrapper.HMOPayment.Update(pendingPayment);
                await _repoWrapper.Save();

                var newJobId = BackgroundJob.Schedule(() => ProcessHygeiaHMOPayment(fromAccount, toAccount, null, null, null), pendingPayment.NextMonthPaymentDate);
                var hmoPayment = new HMOPayment()
                {
                    PaymentDate = pendingPayment.NextMonthPaymentDate,
                    Channel = PaymentReference_ChannelValue.healthinsured_hygeia.ToString(),
                    Status = PaymentReference_StatusValue.Pending.ToString(),
                    NextMonthPaymentDate = pendingPayment.NextMonthPaymentDate.AddDays(1).AddMonths(1).AddDays(-1)
                };
                hmoPayment.JobId = newJobId;
                _repoWrapper.HMOPayment.Create(hmoPayment);
                await _repoWrapper.Save();
            }
            else
            {
                pendingPayment.Status = PaymentReference_StatusValue.Failed.ToString();
                _repoWrapper.HMOPayment.Update(pendingPayment);
                await _repoWrapper.Save();
                _emailSender.CustomMail("Hassan.Hassan@sterling.ng", "Failed HMO Payment", "HMO payment with Id : " + pendingPayment.Id + " to " + pendingPayment.Channel + " Failed. Kindly investigate and retry");
            }
            await Task.CompletedTask;
        }

        public async Task MakeAxamansardHMOPayment()
        {
            var paymentCount = await _repoWrapper.HMOPayment.GetPaymentCount(PaymentReference_ChannelValue.healthinsured_axamansard.ToString());
            if (paymentCount < 1)
            {
                var healthInsuredAcc = HMOAccountDetails.HealthInsuredAccountNumber;
                var axaAcc = HMOAccountDetails.AxamansardAccountNumber;

                var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

                var endOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                var endOfNextMonth = firstDayOfMonth.AddMonths(2).AddDays(-1);

                var hmoPayment = new HMOPayment()
                {
                    PaymentDate = endOfMonth,
                    NextMonthPaymentDate = endOfNextMonth,
                    Channel = PaymentReference_ChannelValue.healthinsured_axamansard.ToString(),
                    Status = PaymentReference_StatusValue.Pending.ToString(),
                };
                var jobId = BackgroundJob.Schedule(() => ProcessAxamansardHMOPayment(healthInsuredAcc, axaAcc, null, null, null), endOfMonth);
                hmoPayment.JobId = jobId;
                _repoWrapper.HMOPayment.Create(hmoPayment);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
        }

        public async Task ProcessAxamansardHMOPayment(string fromAccount, string toAccount, decimal? amount, string getJobId, PerformContext context)
        {
            decimal axaPayment;
            if (amount is null)
            {
                axaPayment = _repoWrapper.PaymentReference.QueryAllPaymentReference()
               .Where(x => x.Status.Equals(PaymentReference_StatusValue.Successful.ToString()) && x.Channel.ToLower() == PaymentReference_ChannelValue.healthinsured_axamansard.ToString().ToLower()
               && x.Amount > decimal.Parse("50") && x.Date.Date.Month == DateTime.Now.Date.Month)
               .Select(x => x.Amount).Sum();
                axaPayment *= decimal.Parse("0.9");
            }
            else
            {
                axaPayment = amount.Value;
            }

            string jobId;
            if (getJobId == null)
            {
                jobId = context.BackgroundJob.Id;
            }
            else
            {
                jobId = getJobId;
            }

            var pendingPayment = await _repoWrapper.HMOPayment.GetPaymentByJobId(jobId);
            var axaPaymentResponse = new OBJ_IBS_Transfer_Response_Class();
            if (axaPayment > 50)
            {
                axaPaymentResponse = await _iBSIntegrationService.SterlingBankIntraBank(axaPayment, toAccount,
                fromAccount, "Automated Sterling Bank monthly payment to Axamansard. HealthBanc- Axamansard partnership ", "NG0020032");
            }
            else
            {
                axaPaymentResponse.ResponseCode = "00";
                axaPaymentResponse.Reference = Guid.NewGuid().ToString();
                axaPaymentResponse.FTReference = "00";
            }

            pendingPayment.Reference = axaPaymentResponse.Reference;
            pendingPayment.PaymentDate = DateTime.Now;
            pendingPayment.Amount = axaPayment;

            if (axaPaymentResponse.ResponseCode == "00")
            {
                pendingPayment.Status = PaymentReference_StatusValue.Successful.ToString();
                pendingPayment.FTReference = axaPaymentResponse.FTReference;
                _repoWrapper.HMOPayment.Update(pendingPayment);
                await _repoWrapper.Save();

                var newJobId = BackgroundJob.Schedule(() => ProcessAxamansardHMOPayment(fromAccount, toAccount, null, null, null), pendingPayment.NextMonthPaymentDate);
                var hmoPayment = new HMOPayment()
                {
                    PaymentDate = pendingPayment.NextMonthPaymentDate,
                    Channel = PaymentReference_ChannelValue.healthinsured_axamansard.ToString(),
                    Status = PaymentReference_StatusValue.Pending.ToString(),
                    NextMonthPaymentDate = pendingPayment.NextMonthPaymentDate.AddDays(1).AddMonths(1).AddDays(-1)
                };
                hmoPayment.JobId = newJobId;
                _repoWrapper.HMOPayment.Create(hmoPayment);
                await _repoWrapper.Save();
            }
            else
            {
                pendingPayment.Status = PaymentReference_StatusValue.Failed.ToString();
                _repoWrapper.HMOPayment.Update(pendingPayment);
                await _repoWrapper.Save();
                _emailSender.CustomMail("Hassan.Hassan@sterling.ng", "Failed HMO Payment", "HMO payment with Id : " + pendingPayment.Id + " to " + pendingPayment.Channel + " Failed. Kindly investigate and retry");
            }
            await Task.CompletedTask;
        }

        public async Task ProcessFailedHMOPayment(int HMOPaymentId)
        {
            var pendingPayment = await _repoWrapper.HMOPayment.GetFailedPaymentById(HMOPaymentId);
            if (pendingPayment.Channel == PaymentReference_ChannelValue.healthinsured_hygeia.ToString())
            {
                var healthInsuredAcc = HMOAccountDetails.HealthInsuredAccountNumber;
                var hygeiaAcc = HMOAccountDetails.HygeiaAccountNumber;
                await ProcessHygeiaHMOPayment(healthInsuredAcc, hygeiaAcc, pendingPayment.Amount, pendingPayment.JobId, null);
            }
            else
            {
                var healthInsuredAcc = HMOAccountDetails.HealthInsuredAccountNumber;
                var axaAcc = HMOAccountDetails.AxamansardAccountNumber;
                await ProcessAxamansardHMOPayment(healthInsuredAcc, axaAcc, pendingPayment.Amount, pendingPayment.JobId, null);
            }
        }

        public async Task FitPaymentError(string reference, string amount)
        {
            var payReference = await _repoWrapper.PaymentReference.GetByReference(reference);
            if (payReference != null)
            {
                payReference.Amount = decimal.Parse(amount);
                _repoWrapper.PaymentReference.Update(payReference);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
        }

        public async Task FixCardsError(string email)
        {
            var user = await _repoWrapper.InsuranceProfile.GetByEmail(email);
            if (user is null)
            {
                var company = await _repoWrapper.CompanyProfile.GetCompanyProfileByEmail(email);
                var cards = company.Cards;
                var primaryCard = company.Cards.Where(x => x.Status == (int)DebitCard_StatusValue.primary).FirstOrDefault();
                var cardErrorList = new List<DebitCard>();
                foreach (var item in cards)
                {
                    if (item.Id != primaryCard.Id && item.LastFourDigit == primaryCard.LastFourDigit)
                    {
                        cardErrorList.Add(item);
                    }
                }
                _repoWrapper.Card.DeleteRange(cardErrorList);
                await _repoWrapper.Save();
            }
            else
            {
                var primaryCard = user.Cards.Where(x => x.Status == (int)DebitCard_StatusValue.primary).FirstOrDefault();
                var cards = user.Cards;
                var cardErrorList = new List<DebitCard>();
                foreach (var item in cards)
                {
                    if (item.Id != primaryCard.Id && item.LastFourDigit == primaryCard.LastFourDigit)
                    {
                        cardErrorList.Add(item);
                    }
                }
                _repoWrapper.Card.DeleteRange(cardErrorList);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
        }

    }
}
