using Application.Common.DTO;
using Application.Common.Interfaces;
using Application.Orders.Commands;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.Data;

namespace Application.Orders;

public class OrderCommandHandler : IRequestHandler<ApproveOrderCommand, BaseResponse>
{
    private readonly ILogger<OrderCommandHandler> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ApplicationDbContext _context;

    public OrderCommandHandler(ILogger<OrderCommandHandler> logger, UserManager<ApplicationUser> userManager, ITokenService tokenService, ApplicationDbContext context)
    {
        _logger = logger;
        _userManager = userManager;
        _tokenService = tokenService;
        _context = context;
    }
    public async Task<BaseResponse> Handle(ApproveOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Approve Subscription Request [SubscriptionId : {request.SubscriptionId}] \n");
        var subscription = await _context.Orders.SingleOrDefaultAsync(x => x.Id == request.SubscriptionId && !x.IsDeleted,cancellationToken);
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
        _context.Orders.Update(subscription);
        await _context.SaveChangesAsync(cancellationToken);
        return BaseResponse.Success();
    }
}
