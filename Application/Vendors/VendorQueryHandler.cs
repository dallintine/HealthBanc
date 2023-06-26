using Application.Common.DTO;
using Application.Vendors.DTO;
using Application.Vendors.Queries;
using AutoMapper;
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
    public class VendorQueryHandler : IRequestHandler<GetVendorListQuery, BaseResponse<List<VendorDTO>>>
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
            var products = await _context.Vendors.Where(x => !x.IsDeleted).ToListAsync();
            var productDTOs = _mapper.Map<List<Vendor>, List<VendorDTO>>(products);
            return BaseResponse<List<VendorDTO>>.Success(productDTOs);
        }
    }
}
