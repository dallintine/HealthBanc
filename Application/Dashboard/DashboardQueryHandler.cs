using Application.Common.DTO;
using Application.Dashboard.DTO;
using Application.Dashboard.Queries;
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

namespace Application.Dashboard;

public class DashboardQueryHandler : IRequestHandler<GetDashboardSummaryQuery, BaseResponse<DashboardSummaryQuery>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public DashboardQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<BaseResponse<DashboardSummaryQuery>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        if(request.StartDate is null)
        {
            request.StartDate = new DateTime();
        }
        var subQuery = _context.Subscriptions.Include(x => x.Plan).Where(x => !x.IsDeleted && x.CreatedAt.Date >= request.StartDate.Value.Date);
        //var customerQuery = _context.Users.Where(x => x.CreatedAt.Date >= request.StartDate.Value.Date && !x.IsDeleted);
        var customerQuery = _context.Users.AsQueryable();
        var vendorQuery = _context.Vendors.Where(x => x.CreatedAt.Date >= request.StartDate.Value.Date && !x.IsDeleted);

        if (request.EndDate != null)
        {
            subQuery = subQuery.Where(x => x.CreatedAt.Date <= request.EndDate.Value.Date);
            //customerQuery = customerQuery.Where(x => x.CreatedAt.Date <= request.EndDate.Value.Date);
            vendorQuery = vendorQuery.Where(x => x.CreatedAt.Date <= request.EndDate.Value.Date);
        }

        var dashboardDTO = new DashboardSummaryQuery
        {
            TotalRevenue = await subQuery.SumAsync(x => x.Amount, cancellationToken),
            TransactionCount = await subQuery.CountAsync(cancellationToken),
            TotalIncome = await subQuery.SumAsync(x => (x.Amount * Convert.ToDecimal(x.Plan.MarkUpRate)), cancellationToken),
            //CustomersCount = await customerQuery.CountAsync(x => !x.Email.Contains(".admin") && !x.IsDeleted,cancellationToken),
            CustomersCount = await customerQuery.CountAsync(x => !x.Email.Contains(".admin"), cancellationToken),
            VendorsCount = await vendorQuery.CountAsync(x => !x.IsDeleted,cancellationToken)
        };

        return BaseResponse<DashboardSummaryQuery>.Success(dashboardDTO);
    }
}
