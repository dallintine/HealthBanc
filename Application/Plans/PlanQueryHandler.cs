using Application.Common.DTO;
using Application.Plans.DTO;
using Application.Plans.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Plans
{
    public class PlanQueryHandler : IRequestHandler<GetPlanListQuery, PageBaseResponse<List<PlanDTO>>>,
        IRequestHandler<GetPlanDescriptionsListQuery, PageBaseResponse<List<PlanDescriptionDTO>>>,
        IRequestHandler<GetBasicCatalogQuery, BaseResponse<List<PlanDTO>>>,
        IRequestHandler<TopDiscountedPlanQuery, BaseResponse<List<TopDiscountedPlanDTO>>>
    {
        private readonly ApplicationDbContext _context;

        public PlanQueryHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageBaseResponse<List<PlanDTO>>> Handle(GetPlanListQuery request, CancellationToken cancellationToken)
        {
            var plans = _context.Plans.Include(x => x.Vendor).Include(x => x.PlanDescriptions).Where(x => !x.IsDeleted).AsQueryable();
            if(request.ServiceId != null)
            {
                plans = plans.Where(x => x.ServiceId == request.ServiceId.Value);
            }
            if (request.VendorId != null)
            {
                plans = plans.Where(x => x.VendorId == request.VendorId.Value);
            }
            if (String.IsNullOrEmpty(request.SearchText))
            {
                plans = plans.Where(x => x.Name.Contains(request.SearchText));
            }
            var planDTOs = await plans.Select(x => new PlanDTO
            {
                Id = x.Id,
                Name = x.Name,
                Price = x.Price,
                ServiceId = x.ServiceId,
                Discount = x.Discount,
                VendorName = x.Vendor.Name,
                ImageURL = x.ImageURL,
                Tag = x.Tag,
                ExternalLinkName = x.ExternalLinkName,
                ExternalLinkURL = x.ExternalLinkURL,
                OptionalFee = x.OptionalFee,
                PlanDescriptionDTOs = x.PlanDescriptions.Where(x => x.IsDeleted == false).Select(y => new PlanDescriptionDTO { Id = y.Id, Name = y.Name }).ToList()
            }).ToListAsync(cancellationToken: cancellationToken);
            return PageBaseResponse<List<PlanDTO>>.Success(planDTOs, 1, request.PageSize, 1, planDTOs.Count);
        }

        public async Task<PageBaseResponse<List<PlanDescriptionDTO>>> Handle(GetPlanDescriptionsListQuery request, CancellationToken cancellationToken)
        {
            var planDescriptions = _context.PlanDescriptions.Where(x => x.PlanId == request.PlanId && !x.IsDeleted).AsQueryable();
            var planDescriptionsDTOs = await planDescriptions.Select(x => new PlanDescriptionDTO
            {
                Id = x.Id,
                Name = x.Name
            }).ToListAsync(cancellationToken: cancellationToken);
            return PageBaseResponse<List<PlanDescriptionDTO>>.Success(planDescriptionsDTOs, 1, request.PageSize, 1, planDescriptionsDTOs.Count);
        }

        public async Task<BaseResponse<List<PlanDTO>>> Handle(GetBasicCatalogQuery request, CancellationToken cancellationToken)
        {
            var plans = await _context.Plans.Include(x => x.Vendor).Include(x => x.PlanDescriptions).Where(x => !x.IsDeleted).GroupBy(x => x.ServiceId)
                .Select(x => x.FirstOrDefault()).ToListAsync(cancellationToken: cancellationToken);

            var planDTOs = plans.Select(x => new PlanDTO
            {
                Id = x.Id,
                Name = x.Name,
                Price = x.Price,
                ServiceId = x.ServiceId,
                Discount = x.Discount,
                VendorName = x.Vendor.Name,
                ImageURL = x.ImageURL,
                Tag = x.Tag,
                ExternalLinkName = x.ExternalLinkName,
                ExternalLinkURL = x.ExternalLinkURL,
                OptionalFee = x.OptionalFee,
                PlanDescriptionDTOs = x.PlanDescriptions.Where(x => x.IsDeleted == false).Select(y => new PlanDescriptionDTO { Id = y.Id, Name = y.Name }).ToList()
            }).ToList();

            return BaseResponse<List<PlanDTO>>.Success(planDTOs);
        }

        public async Task<BaseResponse<List<TopDiscountedPlanDTO>>> Handle(TopDiscountedPlanQuery request, CancellationToken cancellationToken)
        {
            var plans = await _context.Plans.Include(x => x.Vendor).Include(x => x.PlanDescriptions).Where(x => !x.IsDeleted).OrderBy(x => x.Discount).Take(5).ToListAsync();

            var planDTOs = plans.Select(x => new TopDiscountedPlanDTO
            {
                Id = x.Id,
                Name = x.Name,
                Price = x.Price,
                ServiceId = x.ServiceId,
                Discount = x.Discount,
                VendorName = x.Vendor.Name,
                ImageURL = x.ImageURL,
                Tag = x.Tag
            }).ToList();

            return BaseResponse<List<TopDiscountedPlanDTO>>.Success(planDTOs);
        }
    }
}
