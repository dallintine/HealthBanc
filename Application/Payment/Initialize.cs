using Application.CommonDTO;
using Application.Interfaces;
using AutoMapper;
using DataAccess;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Payment
{
    public class Initialize
    {
        public class Command : IRequest<BaseResponse>
        {
            public List<InitializePaymentDTO> InitializePaymentDTOs { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.InitializePaymentDTOs).NotNull().NotEmpty();
            }
        }

        public class Handler : IRequestHandler<Command, BaseResponse>
        {
            private readonly ILogger<Handler> _logger;
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly IRepositoryWrapper _repositoryWrapper;
            private readonly IPaystackService _paystackService;
            private readonly IMapper _mapper;
            private readonly ITokenService _tokenService;

            public Handler(ILogger<Handler> logger, UserManager<ApplicationUser> userManager,IRepositoryWrapper repositoryWrapper, IPaystackService paystackService,
                IMapper mapper,ITokenService tokenService)
            {
                _logger = logger;
                _userManager = userManager;
                _repositoryWrapper = repositoryWrapper;
                _paystackService = paystackService;
                _mapper = mapper;
                _tokenService = tokenService;
            }

            public async Task<BaseResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                var claims = _tokenService.GetClaims();
                var transaction = new Domain.Entities.Transaction
                {
                    Reference = $"HBR{Guid.NewGuid().ToString()}",
                    Email = claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value,
                    ApplicationUserId = long.Parse(claims.FirstOrDefault(x => x.Type == "UserId")?.Value),
                };

                var subscriptionList = new List<Subscription>();
                foreach (var item in request.InitializePaymentDTOs)
                {
                    var plan = await _repositoryWrapper.Plan.Find(x => x.Id == item.PlanId);
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
                        Status = SubscriptionStatus.Terminated.ToString()
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
                transaction.Amount = totalAmount;
                _repositoryWrapper.Transaction.Create(transaction);
                await _repositoryWrapper.Save();

                subscriptionList.ForEach(s => s.TransactionId = transaction.Id);
                _repositoryWrapper.Subscription.CreateRange(subscriptionList);
                await _repositoryWrapper.Save();

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
        }
    }
}
