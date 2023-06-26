using Application.Common.DTO;
using Application.Plans.Commands;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Plans;

public class PlanCommandHandler : IRequestHandler<CreatePlanCommand, BaseResponse>,
    IRequestHandler<UpdatePlanCommand, BaseResponse>,
    IRequestHandler<DeletePlanCommand, BaseResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PlanCommandHandler> _logger;

    public PlanCommandHandler(ApplicationDbContext context, ILogger<PlanCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }
    public  async Task<BaseResponse> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _context.Plans.FirstOrDefaultAsync(x => x.Name.ToLower() == request.Name.ToLower() && x.ServiceId == request.ServiceId && !x.IsDeleted, cancellationToken);
        if (plan != null) return BaseResponse.Failure("26", "Duplicate Record - Plan with this name exist");
        plan = new Plan
        {
            VendorId = request.VendorId,
            Name = request.Name,
            Discount = request.Discount,
            MarkUpRate = request.MarkUpRate,
            Price = request.Price,
            ServiceId = request.ServiceId,
        };
        _context.Plans.Add(plan);
        await _context.SaveChangesAsync(cancellationToken);
        return BaseResponse.Success();
    }

    public async Task<BaseResponse> Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Edit Plan request processing [Payload : {JsonConvert.SerializeObject(request)}] \n");
        var plan = await _context.Plans.SingleOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
        if (plan is null) return BaseResponse.Failure("25", "No record Found");
        plan.Discount = request.Discount;
        plan.MarkUpRate = request.MarkUpRate;
        plan.Price = request.Price;
        plan.Name = request.Name;
        _context.Plans.Update(plan);
        await _context.SaveChangesAsync(cancellationToken);
        return BaseResponse.Success();
    }

    public async Task<BaseResponse> Handle(DeletePlanCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Delete Plan request processing [PlanId : {request.Id}]");
        var plan = await _context.Plans.SingleOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
        if (plan is null) return BaseResponse.Failure("25", "No record found");
        plan.IsDeleted = true;
        _context.Plans.Update(plan);
        await _context.SaveChangesAsync(cancellationToken);
        return BaseResponse.Success();
    }
}
