using Application.Common.DTO;
using Application.Subscriptions.DTO;
using Application.Subscriptions.Queries;
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

namespace Application.Subscriptions
{
    public class DashboardQueryHandler : IRequestHandler<GetSubscriptionListQuery, PageBaseResponse<List<SubscriptionDTO>>>,
        IRequestHandler<GetPaymentSummaryQuery, BaseResponse<PaymentSummaryDTO>> , 
        IRequestHandler<ExportPaymentList , BaseResponse>
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public DashboardQueryHandler(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PageBaseResponse<List<SubscriptionDTO>>> Handle(GetSubscriptionListQuery request, CancellationToken cancellationToken)
        {
            if (request.StartDate is null)
            {
                request.StartDate = new DateTime();
            }
            Func<Subscription, bool> query = x =>
                  (string.IsNullOrEmpty(request.Status) || x.Status.ToLower() == request.Status.ToLower());
            var querySubscriptions = _context.Subscriptions.Include(x => x.ApplicationUser).Include(x => x.Plan).Include(x => x.Plan.Vendor).Where(query);

            querySubscriptions = querySubscriptions.Where(x => x.CreatedAt.Date >= request.StartDate.Value.Date);
            if (request.EndDate != null)
            {
                querySubscriptions = querySubscriptions.Where(x => x.CreatedAt.Date <= request.EndDate.Value.Date);
            }

            if (!String.IsNullOrEmpty(request.SearchText))
            {
                querySubscriptions = querySubscriptions.Where(x => x.ApplicationUser.FirstName.ToLower().Contains(request.SearchText.ToLower()) 
                || x.ApplicationUser.LastName.ToLower().Contains(request.SearchText.ToLower()) || x.Plan.Name.ToLower().Contains(request.SearchText.ToLower())
                || x.Plan.Vendor.Name.ToLower().Contains(request.SearchText.ToLower()) || x.Amount.ToString().Contains(request.SearchText));
            }
            var skip = (request.PageNumber - 1) * request.PageSize;

            var filteredQueryable = querySubscriptions.Skip(skip).Take(request.PageSize).AsQueryable();
            var recordCount = querySubscriptions.Count();
            var pageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)request.PageSize));
            var pageNumber = request.PageNumber >= 1 ? request.PageNumber : (int?)null;
            var pageSize = request.PageSize >= 1 ? request.PageSize : (int?)null;
            var subscriptions = filteredQueryable.ToList();
            var data = _mapper.Map<List<Subscription>, List<SubscriptionDTO>>(subscriptions);
            return PageBaseResponse<List<SubscriptionDTO>>.Success(data, pageNumber, pageSize, pageCount, recordCount);
        }

        public async Task<BaseResponse<PaymentSummaryDTO>> Handle(GetPaymentSummaryQuery request, CancellationToken cancellationToken)
        {

            var subQuery = _context.Subscriptions.Include(x => x.Plan).Where(x => !x.IsDeleted);
            var summaryDTO = new PaymentSummaryDTO
            {
                TotalRevenue = await subQuery.SumAsync(x => x.Amount, cancellationToken),
                TransactionCount = await subQuery.CountAsync(cancellationToken),
                TotalIncome = await subQuery.SumAsync(x => (x.Amount * Convert.ToDecimal(x.Plan.MarkUpRate)), cancellationToken),
            };

            return BaseResponse<PaymentSummaryDTO>.Success(summaryDTO);

        }

        public async Task<BaseResponse> Handle(ExportPaymentList request, CancellationToken cancellationToken)
        {
            if (request.StartDate is null)
            {
                request.StartDate = new DateTime();
            }
            Func<Subscription, bool> query = x =>
                  (string.IsNullOrEmpty(request.Status) || x.Status.ToLower() == request.Status.ToLower());
            var querySubscriptions = _context.Subscriptions.Include(x => x.ApplicationUser).Include(x => x.Plan).Include(x => x.Plan.Vendor).Where(query);

            querySubscriptions = querySubscriptions.Where(x => x.CreatedAt.Date >= request.StartDate.Value.Date);
            if (request.EndDate != null)
            {
                querySubscriptions = querySubscriptions.Where(x => x.CreatedAt.Date <= request.EndDate.Value.Date);
            }
            return BaseResponse.Success("Payment data would be processed and sent to your email address");
        }
    }
}
