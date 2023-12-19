using Application.Common.ConfigSettings;
using Application.Common.DTO;
using Application.Common.Interfaces;
using Application.Payment.Commands;
using Application.Payment.DTO;
using Domain.Entities;
using Domain.Enums;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Payment
{
    public class PaymentCommandHandler : IRequestHandler<CallbackCommand, BaseResponse>,
        IRequestHandler<InitializeCommand, BaseResponse>,
        IRequestHandler<PaystackWebHookCommand,BaseResponse>

    {
        private readonly ILogger<PaymentCommandHandler> _logger;
        private readonly IPaystackService _paystackService;
        private readonly ApplicationDbContext _context;
        private readonly ITemplateService _templateService;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;
        private readonly ITokenService _tokenService;
        private readonly EmailSettings _emailSettings;
        private readonly PaystackSettings _paystackSettings;

        public PaymentCommandHandler(ILogger<PaymentCommandHandler> logger, IPaystackService paystackService, ApplicationDbContext context,
            ITemplateService templateService, IWebHostEnvironment environment, IEmailService emailService, ITokenService tokenService,IOptions<PaystackSettings> paystackSettings,
            IOptions<EmailSettings> emailSettings)
        {
            _logger = logger;
            _paystackService = paystackService;
            _context = context;
            _templateService = templateService;
            _environment = environment;
            _emailService = emailService;
            _tokenService = tokenService;
            _emailSettings = emailSettings.Value;
            _paystackSettings = paystackSettings.Value;
        }

        public async Task<BaseResponse> Handle(PaystackWebHookCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var paystackIpaddress = new List<string>()
                {
                    "52.49.173.169","52.214.14.220","52.31.139.75"
                };
                var ipAddress = _tokenService.GetIP();
                if (paystackIpaddress.Contains(ipAddress))
                {
                    _logger.LogInformation($"WebbHokk Command Request [Reference : {request.Data.Reference} | {JsonConvert.SerializeObject(request)} ]");
                    if(request.@event.ToLower() == "charge.success".ToLower())
                    {
                        var transaction = await _context.Transactions.Include(x => x.ApplicationUser).Include(x => x.Subscriptions).ThenInclude(x => x.Plan).ThenInclude(x => x.Vendor)
                            .ThenInclude(x => x.Service).SingleOrDefaultAsync(x => x.Reference == request.Data.Reference);
                        if (transaction != null)
                        {
                            transaction.IsCompleted = true;
                            foreach (var subscription in transaction.Subscriptions)
                            {
                                subscription.Status = SubscriptionStatus.Pending.ToString();
                                subscription.IsSuccessful = true;
                                subscription.UpdatedAt = DateTime.Now;
                            }
                            _context.Transactions.Update(transaction);
                            await _context.SaveChangesAsync();

                            var invoiceItems = transaction.Subscriptions.Select(x => new InvoiceItem
                            {
                                ServiceName = x.Plan.Service.Name,
                                PlanName = x.Plan.Name,
                                Price = x.Amount,
                                Quantity = x.Quantity,
                                ProductName = x.Plan.Name,
                                VendorName = x.Plan.Vendor.Name,
                                TotalPrice = (x.Amount * x.Quantity),
                                TransactionDate = x.CreatedAt.ToLocalTime().ToString(),
                                TransactionId = x.PaymentReference
                            }).ToList();

                            var name = $"{transaction.ApplicationUser.FirstName} {transaction.ApplicationUser.LastName}";

                            BackgroundJob.Enqueue(() => EnqueSuccesfulTransaction(invoiceItems, transaction.ApplicationUser.PhoneNumber, transaction.ApplicationUser.Email,name));
                        }
                    }
                }
                return BaseResponse.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occured while sending Webhook Exception : ${ex.ToString()}");
                return BaseResponse.Success();
            }       
        }

        public async Task<BaseResponse> Handle(CallbackCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Callback Command Request [Reference : {request.Reference} ]");
            var validatePayment = await _paystackService.VerifyPayment(request.Reference);
            if (!validatePayment.Status) return BaseResponse.Failure("06", validatePayment.Message);

            var transaction = await _context.Transactions.Include(x => x.ApplicationUser).Include(x => x.Subscriptions).ThenInclude(x => x.Plan).ThenInclude(x => x.Vendor)
                .ThenInclude(x => x.Service).SingleOrDefaultAsync(x => x.Reference == request.Reference);
            if (transaction != null)
            {
                transaction.IsCompleted = true;
                foreach (var subscription in transaction.Subscriptions)
                {
                    subscription.Status = SubscriptionStatus.Pending.ToString();
                    subscription.IsSuccessful = true;
                    subscription.UpdatedAt = DateTime.Now;
                }
                _context.Transactions.Update(transaction);
                await _context.SaveChangesAsync();

                var invoiceItems = transaction.Subscriptions.Select(x => new InvoiceItem
                {
                    ServiceName = x.Plan.Service.Name,
                    PlanName = x.Plan.Name,
                    Price = x.Amount,
                    Quantity = x.Quantity,
                    TotalPrice = (x.Amount * x.Quantity),
                    ProductName = x.Plan.Name,
                    VendorName = x.Plan.Vendor.Name,
                    TransactionDate = x.CreatedAt.ToLocalTime().ToString(),
                    TransactionId = x.PaymentReference
                }).ToList();

                var subTotal = invoiceItems.Sum(x => x.TotalPrice);
                var transactionCharge = (_paystackSettings.Charges * subTotal);
                var name = $"{transaction.ApplicationUser.FirstName} {transaction.ApplicationUser.LastName}";

                var pdfHTMLTemplate = await _templateService.RenderAsync("Client/Invoice", new InvoicePDFTemplateDTO
                {
                    Name = name,
                    InvoiceItems = invoiceItems,
                    SubTotal = subTotal,
                    TransactionCharge = transactionCharge,
                    Total = subTotal + transactionCharge
                });

                //var pdfFile = _templateService.GeneratePDF_ParseXhtml(pdfHTMLTemplate);

                await _emailService.EmailRequest(new EmailRequest
                {
                    Subject = "Payment Success! Your Healthbanc Purchase Details",
                    Message = pdfHTMLTemplate,
                    Email = transaction.ApplicationUser.Email
                });

                return BaseResponse.Success();
            }
            return BaseResponse.Failure("25", "Transaction was not found");
        }

        public async Task<BaseResponse> Handle(InitializeCommand request, CancellationToken cancellationToken)
        {
            var claims = _tokenService.GetClaims();
            long userId = long.Parse(claims.FirstOrDefault(x => x.Type == "UserId")?.Value);
            _logger.LogInformation($"Intialize Payment Command [Payload : {JsonConvert.SerializeObject(request)} | UserId :  {userId}]");
            var transaction = new Domain.Entities.Transaction
            {
                Reference = $"HBR{Guid.NewGuid().ToString()}",
                Email = claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value,
                ApplicationUserId = userId
            };

            var subscriptionList = new List<Subscription>();
            foreach (var item in request.InitializePaymentDTOs)
            {
                var plan = await _context.Plans.FindAsync(item.PlanId);
                if (plan is null) return BaseResponse.Failure("25", "No plan record found");
                subscriptionList.Add(new Subscription
                {
                    ApplicationUserId = transaction.ApplicationUserId,
                    PlanId = plan.Id,
                    ServiceId = plan.ServiceId,
                    VendorId = plan.VendorId,
                    IsSuccessful = false,
                    Amount = ((plan.Price - plan.Discount) * item.Quantity),
                    OptionalFee = plan.OptionalFee,
                    Quantity = item.Quantity,
                    Status = SubscriptionStatus.Terminated.ToString(),
                    PaymentReference = transaction.Reference
                });
            };

            // Check for multiple plans of the same product
            var groupedSubList = subscriptionList.GroupBy(x => x.ServiceId);
            if (groupedSubList.Count() != subscriptionList.Count(x => !x.OptionalFee))
            {
                return BaseResponse.Failure("06", "Plans of the same service cannot be purchased or added to the cart at the same time");
            }
            // calculate total sum
            var totalAmount = subscriptionList.Sum(x => (x.Amount));
            totalAmount += ( _paystackSettings.Charges * totalAmount);
            totalAmount = Math.Round(totalAmount, 2);
            transaction.Amount = totalAmount;
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            subscriptionList.ForEach(s => s.TransactionId = transaction.Id);
            _context.Subscriptions.AddRange(subscriptionList);
            await _context.SaveChangesAsync();

            var paymentResponse = new InitializePaymentResponse();
            var initializePaymentrequest = new InitializePaymentRequest
            {
                Amount = (totalAmount * 100).ToString(),
                Reference = transaction.Reference,
                Metadata = new Metadata
                {
                    Custom_fields = subscriptionList.Select(x => new CustomField { SubscriptionId = x.Id }).ToList()
                }
            };

            paymentResponse = await _paystackService.InitlilizePayment(initializePaymentrequest);
            if (!paymentResponse.Status)
            {
                return BaseResponse.Failure("06", "Could not initialise subscription process");
            }
            paymentResponse.Data.Amount = initializePaymentrequest.Amount;
            paymentResponse.Data.Email = _tokenService.GetClaims().FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
            return BaseResponse<InitializePaymentResponse>.Success(paymentResponse);
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task EnqueSuccesfulTransaction(List<InvoiceItem> invoiceItems, string userPhonenumber , string userEmail, string userName)
        {

            BackgroundJob.Schedule(() => _emailService.PaymentConfirmationEmail(userName, userEmail), DateTimeOffset.Now.AddMinutes(1));

            var gymInvoiceItem = invoiceItems.Where(x => x.ServiceName.ToLower().Replace(" ", "") == ServicesEnum.Physicals.ToString().ToLower()).FirstOrDefault();
            if (gymInvoiceItem != null)
            {
                await SendPartnerEmail(gymInvoiceItem, userEmail, userPhonenumber, userName, _emailSettings.IFitnessEmail);
                BackgroundJob.Schedule(() => _emailService.PlanStepsEmail(userName, userEmail, ServicesEnum.Physicals.ToString(),
                    gymInvoiceItem.VendorName, gymInvoiceItem.ProductName, gymInvoiceItem.ServiceName), DateTimeOffset.Now.AddMinutes(3));
            }

            var mealInvoiceItem = invoiceItems.Where(x => x.ServiceName.ToLower().Replace(" ", "") == ServicesEnum.HeathlyMeal.ToString().ToLower()).FirstOrDefault();
            if (mealInvoiceItem != null)
            {
                await SendPartnerEmail(mealInvoiceItem, userEmail, userPhonenumber, userName, _emailSettings.SoFreshEmail);
                BackgroundJob.Schedule(() => _emailService.PlanStepsEmail(userName, userEmail, ServicesEnum.HeathlyMeal.ToString(),
                    mealInvoiceItem.VendorName, mealInvoiceItem.ProductName, mealInvoiceItem.ServiceName), DateTimeOffset.Now.AddMinutes(3));
            }

            var diagniosticInvoiceItem = invoiceItems.Where(x => x.ServiceName.ToLower().Replace(" ", "") == ServicesEnum.Diagnostics.ToString().ToLower()).FirstOrDefault();
            if (diagniosticInvoiceItem != null)
            {
                await SendPartnerEmail(diagniosticInvoiceItem, userEmail, userPhonenumber, userName, _emailSettings.HealthtrackerEmail);
                BackgroundJob.Schedule(() => _emailService.PlanStepsEmail(userName, userEmail, ServicesEnum.Diagnostics.ToString(),
                    diagniosticInvoiceItem.VendorName, diagniosticInvoiceItem.ProductName, diagniosticInvoiceItem.ServiceName), DateTimeOffset.Now.AddMinutes(3));
            }
        }

        public async Task SendPartnerEmail(InvoiceItem invoiceItem,string email, string phonenumber, string name, string deliveryEmail)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "/partnerTemplate.html";
            var htmlTemplate = File.ReadAllText(path);
            var emailTemplate = htmlTemplate.Replace("{{VendorName}}", invoiceItem.VendorName).Replace("{{Name}}", name)
                .Replace("{{Email}}", email).Replace("{{PhoneNumber}}", phonenumber ?? " ").Replace("{{ProductName}}", invoiceItem.ProductName)
                .Replace("{{Date}}", invoiceItem.TransactionDate).Replace("{{TransactionId}}", invoiceItem.TransactionId);

            await _emailService.EmailRequest(new EmailRequest
            {
                Subject = "Healthbanc: New Purchase Notification",
                Message = emailTemplate,
                Email = deliveryEmail
            });
        }
    }
}
