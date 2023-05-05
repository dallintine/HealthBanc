using Application.CommonDTO;
using Application.Interfaces;
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

namespace Application.Subscriptions
{
    public class Approve
    {
        public class Command : IRequest<BaseResponse>
        {
            public long SubscriptionId { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.SubscriptionId).NotEmpty().NotNull();
            }
        }

        public class Handler : IRequestHandler<Command, BaseResponse>
        {
            private readonly ILogger<Handler> _logger;
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly ITokenService _tokenService;
            private readonly IRepositoryWrapper _repositoryWrapper;

            public Handler(ILogger<Handler> logger, UserManager<ApplicationUser> userManager, ITokenService tokenService, IRepositoryWrapper repositoryWrapper)
            {
                _logger = logger;
                _userManager = userManager;
                _tokenService = tokenService;
                _repositoryWrapper = repositoryWrapper;
            }

            public async Task<BaseResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                _logger.LogInformation($"Approve Subscription Request [SubscriptionId : {request.SubscriptionId}]");
                var subscription = await _repositoryWrapper.Subscription.Find(x => x.Id == request.SubscriptionId);
                if (subscription is null)
                {
                    _logger.LogInformation($"Approve Subscription Termianted [Reason : Subscription Not Found]");
                    return BaseResponse.Failure("25", "Subscription is not found");
                }
                if (!subscription.IsSuccessful)
                {
                    _logger.LogInformation($"Approve Subscription Termianted [Reason : Subscription is not successfully]");
                    return BaseResponse.Failure("06", "Subscription was not successfully");
                }
                subscription.Status = SubscriptionStatus.Approved.ToString();
                _repositoryWrapper.Subscription.Update(subscription);
                await _repositoryWrapper.Save();
                return BaseResponse.Success();
            }
        }
    }
}
