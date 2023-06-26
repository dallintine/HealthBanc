using Application.Common.DTO;
using Application.Common.Interfaces;
using Application.Subscriptions.Commands;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Subscriptions;

public class SubscriptionCommandHandler : IRequestHandler<ApproveSubscriptionCommand, BaseResponse>
{
    private readonly ILogger<SubscriptionCommandHandler> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ApplicationDbContext _context;

    public SubscriptionCommandHandler(ILogger<SubscriptionCommandHandler> logger, UserManager<ApplicationUser> userManager, ITokenService tokenService, ApplicationDbContext context)
    {
        _logger = logger;
        _userManager = userManager;
        _tokenService = tokenService;
        _context = context;
    }
    public async Task<BaseResponse> Handle(ApproveSubscriptionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Approve Subscription Request [SubscriptionId : {request.SubscriptionId}] \n");
        var subscription = await _context.Subscriptions.SingleOrDefaultAsync(x => x.Id == request.SubscriptionId && !x.IsDeleted,cancellationToken);
        if (subscription is null)
        {
            _logger.LogInformation($"Approve Subscription Termianted [Reason : Subscription Not Found] \n");
            return BaseResponse.Failure("25", "Subscription is not found");
        }
        if (!subscription.IsSuccessful)
        {
            _logger.LogInformation($"Approve Subscription Termianted [Reason : Subscription is not successfully] \n");
            return BaseResponse.Failure("06", "Subscription was not successfully");
        }
        subscription.Status = SubscriptionStatus.Approved.ToString();
        _context.Subscriptions.Update(subscription);
        await _context.SaveChangesAsync(cancellationToken);
        return BaseResponse.Success();
    }
}
