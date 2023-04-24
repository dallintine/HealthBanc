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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Payment
{
    public class Initialize
    {
        public class Command : IRequest<BaseResponse>
        {
            public List<long> PlanIds { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.PlanIds).NotNull().NotEmpty();
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
                var planList = new List<Plan>();

                // Chek that plan id is correct
                foreach (var item in request.PlanIds)
                {
                    var plan = await _repositoryWrapper.Plan.Find(x => x.Id == item);
                    if (plan is null) return BaseResponse.Failure("25", "No plan record found");
                    planList.Add(plan);
                }
                // Check for multiple plans of the same product
                var groupedPlanList = planList.GroupBy(x => x.ProductId);
                if (groupedPlanList.Count() != planList.Count())
                {
                    return BaseResponse.Failure("06", "Plans of the same product cannot be purchased");
                }
                // calculate total sum
                var totalAmount = planList.Sum(x => (x.Price - x.Discount));

                var subscriptionList = new List<Subscription>();
                foreach (var item in subscriptionList)
                {
                    subscriptionList.Add(new Subscription
                    {
                        ApplicationUserId = 2,
                        PlanId = item.PlanId,
                        ProductId = item.ProductId,
                        IsSuccessfully = false,
                        Amount = item.Amount,
                        Status = SubscriptionStatus.Pending.ToString()
                    });
                };
                _repositoryWrapper.Subscription.CreateRange(subscriptionList);

                var paymentResponse = new InitializePaymentResponse();
                var initializePaymentrequest = new InitializePaymentRequest
                {
                    Amount = (totalAmount * 100).ToString(),
                    Reference = Guid.NewGuid().ToString(),
                    Metadata = new Metadata
                    {
                        Custom_fields = subscriptionList.Select(x => new CustomField { SubscriptionId = x.Id}).ToList()
                    }
                };
                paymentResponse = await _paystackService.InitlilizePayment(initializePaymentrequest);
                if (!paymentResponse.Status)
                {
                    await _repositoryWrapper.Save();
                    return BaseResponse.Failure("06", "Could not initialise subscription process");
                }
                subscriptionList.ForEach(x => x.IsSuccessfully = true);
                await _repositoryWrapper.Save();
                return BaseResponse<InitializePaymentResponse>.Success(paymentResponse);               
            }
        }
    }
}
