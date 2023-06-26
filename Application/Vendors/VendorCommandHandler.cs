using Application.Common.DTO;
using Application.Vendors.Commands;
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

namespace Application.Vendors;

public class VendorCommandHandler : IRequestHandler<CreateVendorCommand, BaseResponse>,
    IRequestHandler<UpdateVendorCommand, BaseResponse>,
    IRequestHandler<DeleteVendorCommand, BaseResponse>
{
    private readonly ILogger<VendorCommandHandler> _logger;
    private readonly ApplicationDbContext _context;

    public VendorCommandHandler(ILogger<VendorCommandHandler> logger , ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<BaseResponse> Handle(CreateVendorCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Create Vendor request processing [Payload : {JsonConvert.SerializeObject(request)}] \n");
        var vendor = await _context.Vendors.FirstOrDefaultAsync(x => x.Name.ToLower() == request.Name.ToLower() && !x.IsDeleted, cancellationToken);
        if (vendor != null) return BaseResponse.Failure("26", "Duplicate Record - Vendor with this name exist \n");
        var service = await _context.Services.SingleOrDefaultAsync( x => x.Id == request.ServiceId && !x.IsDeleted, cancellationToken);
        if(service is null)
        {
            _logger.LogInformation($"Create Vendor request terminated [Reason : Invalid Service Id] \n");
            return BaseResponse.Failure("25", "Service not found");
        }
        vendor = new Vendor
        {
            Name = request.Name,
            SettlementAccount = request.SettlementAccount,
        };
        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync(cancellationToken);
        return BaseResponse.Success();
    }

    public async Task<BaseResponse> Handle(UpdateVendorCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Edit Vendor request processing [Payload : {JsonConvert.SerializeObject(request)}] \n");
        var vendor = await _context.Vendors.SingleOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
        if (vendor is null) return BaseResponse.Failure("25", "No record found");
        vendor.Name = request.Name;
        vendor.SettlementAccount = request.SettlementAccount;
        _context.Vendors.Update(vendor);
        await _context.SaveChangesAsync(cancellationToken);
        return BaseResponse.Success();
    }

    public async Task<BaseResponse> Handle(DeleteVendorCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Delete Vendor request processing [ProductId : {request.Id}] \n");
        var vendor = await _context.Vendors.Include(x => x.Plans).Where(x => x.Id == request.Id && !x.IsDeleted).SingleOrDefaultAsync(cancellationToken);
        if (vendor is null) return BaseResponse.Failure("25", "No record found");
        vendor.IsDeleted = true;
        vendor.Plans.ForEach(x => x.IsDeleted = true);
        _context.Vendors.Update(vendor);
        await _context.SaveChangesAsync(cancellationToken);
        return BaseResponse.Success();
    }
}
