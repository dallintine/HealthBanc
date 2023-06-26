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
    public class PlanQueryHandler : IRequestHandler<GetPlanListQuery, PageBaseResponse<List<PlanDTO>>>
    {
        private readonly ApplicationDbContext _context;

        public PlanQueryHandler(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<PageBaseResponse<List<PlanDTO>>> Handle(GetPlanListQuery request, CancellationToken cancellationToken)
        {
            var plans = _context.Plans.Include(x => x.Vendor).Include(x => x.PlanDescriptions).Where(x => x.ServiceId == request.ServiceId && !x.IsDeleted).AsQueryable();
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
    }
}
