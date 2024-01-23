using Application.Common.DTO;
using Application.Vendors.DTO;
using Application.Vendors.Queries;
using AutoMapper;
using DocumentFormat.OpenXml.Wordprocessing;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Vendors
{
    public class VendorQueryHandler : IRequestHandler<GetVendorListQuery, BaseResponse<List<VendorDTO>>>, 
     IRequestHandler<GetVendorQuery, BaseResponse<VendorDTO>>,
     IRequestHandler<GetVendorPurchaseHistory, BaseResponse<VendorPurchaseHistoryDTO>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public VendorQueryHandler(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<BaseResponse<List<VendorDTO>>> Handle(GetVendorListQuery request, CancellationToken cancellationToken)
        {
            var vendors = await _context.Vendors.Where(x => !x.IsDeleted).ToListAsync();
            var vendorDTOs = _mapper.Map<List<Vendor>, List<VendorDTO>>(vendors);
            return BaseResponse<List<VendorDTO>>.Success(vendorDTOs);
        }

        public async Task<BaseResponse<VendorDTO>> Handle(GetVendorQuery request, CancellationToken cancellationToken)
        {
            var vendor = await _context.Vendors.SingleOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (vendor is null) return BaseResponse<VendorDTO>.Failure(null,"25", "Vendor not found");
            var vendorDTO = _mapper.Map<VendorDTO>(vendor);
            return BaseResponse<VendorDTO>.Success(vendorDTO);
        }

        public async Task<BaseResponse<VendorPurchaseHistoryDTO>> Handle(GetVendorPurchaseHistory request, CancellationToken cancellationToken)
        {
            var vendor = await _context.Vendors.Include(x => x.Orders).SingleOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (vendor is null) return BaseResponse<VendorPurchaseHistoryDTO>.Failure(null, "25", "Vendor not found");

            var history = new VendorPurchaseHistoryDTO
            {
                CreatedAt = vendor.CreatedAt,
                PurchaseCount = vendor.Orders.Where(x => x.IsSuccessful).Count(),
            };
            return BaseResponse<VendorPurchaseHistoryDTO>.Success(history);
        }
    }
}
