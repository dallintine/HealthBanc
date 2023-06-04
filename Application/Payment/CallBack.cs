using Application.CommonDTO;
using Application.Interfaces;
using DataAccess;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Payment
{
    public class CallBack
    {
        public class Command : IRequest<BaseResponse>
        {
            [JsonProperty("reference")]
            public string Reference { get; set; }
        }

        public class Handler : IRequestHandler<Command, BaseResponse>
        {
            private readonly ILogger<Handler> _logger;
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly IRepositoryWrapper _repositoryWrapper;
            private readonly IPaystackService _paystackService;
            private readonly ITemplateService _templateService;
            private readonly IWebHostEnvironment _environment;
            private readonly IEmailService _emailService;

            public Handler(ILogger<Handler> logger, UserManager<ApplicationUser> userManager, IRepositoryWrapper repositoryWrapper, IPaystackService paystackService,
                ITemplateService templateService , IWebHostEnvironment environment ,IEmailService emailService)
            {
                _logger = logger;
                _userManager = userManager;
                _repositoryWrapper = repositoryWrapper;
                _paystackService = paystackService;
                _templateService = templateService;
                _environment = environment;
                _emailService = emailService; 
            }

            public async Task<BaseResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                var validatePayment = await _paystackService.VerifyPayment(request.Reference);
                if (!validatePayment.Status) return BaseResponse.Failure("06", validatePayment.Message);

                var transaction = await _repositoryWrapper.Transaction.FindByReference(request.Reference);
                if(transaction != null)
                {
                    transaction.IsCompleted = true;
                    foreach (var subscription in transaction.Subscriptions)
                    {
                        subscription.Status = SubscriptionStatus.Pending.ToString();
                        subscription.IsSuccessful = true;
                        subscription.UpdatedAt = DateTime.Now;
                    }
                    _repositoryWrapper.Transaction.Update(transaction);
                    await _repositoryWrapper.Save();

                    var invoiceItems = transaction.Subscriptions.Select(x => new InvoiceItem
                    {
                        ServiceName = x.Plan.Service.Name,
                        PlanName = x.Plan.Name,
                        Price = x.Amount,
                        Quantity = x.Quantity,
                        TotalPrice = (x.Amount * x.Quantity)
                    }).ToList();

                    var pdfHTMLTemplate = await _templateService.RenderAsync("Client/Invoice", new InvoicePDFTemplateDTO
                    {
                        Name = $"{transaction.ApplicationUser.FirstName} {transaction.ApplicationUser.LastName}",
                        InvoiceItems = invoiceItems,
                        SubTotal = invoiceItems.Sum(x => x.TotalPrice)
                    });

                    var pdfFile = _templateService.GeneratePDF_ParseXhtml(pdfHTMLTemplate);

                    await _emailService.EmailRequest(new EmailRequest
                    {
                        Subject = "Healthbanc Invoice",
                        Message = pdfHTMLTemplate,
                        Email = transaction.ApplicationUser.Email
                    });

                    return BaseResponse.Success();
                }
                return BaseResponse.Failure("25", "Transaction was not found");
            }
        }
    }
}
