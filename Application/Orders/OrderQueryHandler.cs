using Application.Common.DTO;
using Application.Common.Interfaces;
using Application.Orders.DTO;
using Application.Orders.Queries;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;
using System.Security.Claims;

namespace Application.Orders
{
    public class DashboardQueryHandler : IRequestHandler<GetOrdersListQuery, PageBaseResponse<List<OrderDTO>>>,
        IRequestHandler<GetPaymentSummaryQuery, BaseResponse<PaymentSummaryDTO>> , 
        IRequestHandler<ExportPaymentList , BaseResponse<byte[]>>,
        IRequestHandler<CustomerPurchaseAnalyticsQuery , BaseResponse<CustomerPurchaseAnalyticsDTO>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;
        private readonly ITokenService _tokenService;

        public DashboardQueryHandler(ApplicationDbContext context, IMapper mapper,IFileService fileService,ITokenService tokenService)
        {
            _context = context;
            _mapper = mapper;
            _fileService = fileService;
            _tokenService = tokenService;
        }

        public async Task<PageBaseResponse<List<OrderDTO>>> Handle(GetOrdersListQuery request, CancellationToken cancellationToken)
        {
            var roles = _tokenService.GetClaims().Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).ToList();
            if (roles.Any(x => x.ToLower() == Roles.User.ToString().ToLower()))
            {
                var userId = _tokenService.GetClaims().FirstOrDefault(x => x.Type == "UserId")?.Value;
                request.UserId = long.Parse(userId);
            }

            request.StartDate ??= new DateTime();
            Func<Order, bool> query = x =>
                  (!string.IsNullOrEmpty(request.Status) || x.Status.ToLower() == request.Status.ToLower())
                   && (request.VendorId == null || x.VendorId == request.VendorId)
                   && (request.PlanId == null || x.PlanId == request.PlanId)
                   && (request.UserId == null || x.ApplicationUserId == request.UserId);

            var querySubscriptions = _context.Orders.Include(x => x.ApplicationUser).Include(x => x.Plan).Include(x => x.Plan.Vendor).Where(query);

            querySubscriptions = querySubscriptions.Where(x => x.CreatedAt.Date >= request.StartDate.Value.Date);
            if (request.EndDate != null)
            {
                querySubscriptions = querySubscriptions.Where(x => x.CreatedAt.Date <= request.EndDate.Value.Date);
            }

            querySubscriptions = querySubscriptions.OrderByDescending(x => x.CreatedAt);

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
            var data = _mapper.Map<List<Order>, List<OrderDTO>>(subscriptions);
            return PageBaseResponse<List<OrderDTO>>.Success(data, pageNumber, pageSize, pageCount, recordCount);
        }

        public async Task<BaseResponse<PaymentSummaryDTO>> Handle(GetPaymentSummaryQuery request, CancellationToken cancellationToken)
        {

            var subQuery = _context.Orders.Include(x => x.Plan).Where(x => !x.IsDeleted);
            var summaryDTO = new PaymentSummaryDTO
            {
                TotalRevenue = await subQuery.Where(x => x.IsSuccessful).SumAsync(x => x.Amount, cancellationToken),
                TransactionCount = await subQuery.CountAsync(cancellationToken),
                SuccessfulTransactionCount = await subQuery.Where(x => x.IsSuccessful).CountAsync(cancellationToken),
                TotalIncome = await subQuery.Where(x => x.IsSuccessful).SumAsync(x => (x.Amount * Convert.ToDecimal(x.Plan.MarkUpRate)), cancellationToken),
            };

            return BaseResponse<PaymentSummaryDTO>.Success(summaryDTO);

        }

        public async Task<BaseResponse<byte[]>> Handle(ExportPaymentList request, CancellationToken cancellationToken)
        {
            if (request.StartDate is null)
            {
                request.StartDate = new DateTime();
            }
            Func<Order, bool> query = x =>
                  (!string.IsNullOrEmpty(request.Status) || x.Status.ToLower() == request.Status.ToLower());
            var querySubscriptions = _context.Orders.Include(x => x.ApplicationUser).Include(x => x.Plan).Include(x => x.Plan.Vendor).Where(query);

            querySubscriptions = querySubscriptions.Where(x => x.CreatedAt.Date >= request.StartDate.Value.Date);
            if (request.EndDate != null)
            {
                querySubscriptions = querySubscriptions.Where(x => x.CreatedAt.Date <= request.EndDate.Value.Date);
            }

            var customersData = querySubscriptions.Select(x => new ExportOrderDTO
            {
                UserId = x.ApplicationUser.Id != 0 ? x.Id.ToString().PadLeft(6, '0') : null,
                FirstName = x.ApplicationUser.FirstName,
                LastName = x.ApplicationUser.LastName,
                Email = x.ApplicationUser.Email,
                PhoneNumber = x.ApplicationUser.PhoneNumber,
                CreatedAt = x.CreatedAt.ToShortDateString(),
                Amount = x.Amount.ToString(),
                Status = x.Status,
                PaymentReference = x.PaymentReference,
                Plan = x.Plan.Name,
                Vendor = x.Plan.Vendor.Name,
                IsSuccessful = x.IsSuccessful
            }).ToList();
            var fileData = _fileService.GenerateGenericExcelFile(customersData, $"customerdata-{Guid.NewGuid().ToString()}");
            return BaseResponse<byte[]>.Success(fileData, "00", "Data would be processed and sent to your email address");
        }

        public async Task<BaseResponse<CustomerPurchaseAnalyticsDTO>> Handle(CustomerPurchaseAnalyticsQuery request, CancellationToken cancellationToken)
        {
            var userId = _tokenService.GetClaims().FirstOrDefault(x => x.Type == "UserId")?.Value;

            var subscriptions = _context.Orders.Where(x => x.ApplicationUserId == long.Parse(userId) && x.IsSuccessful);

            var analytics = new CustomerPurchaseAnalyticsDTO
            {
                TotalPurchases = subscriptions.Count(),
                TotalAmount = subscriptions.Sum(x => x.Amount),
                VendorsPatronised = subscriptions.Select(x => x.VendorId).Distinct().Count()
            };

            return BaseResponse<CustomerPurchaseAnalyticsDTO>.Success(analytics);

        }
    }
}